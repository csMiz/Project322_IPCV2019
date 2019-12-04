Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

''' <summary>
''' Week7: Wetershed, Channel Split
''' </summary>
Module W7

    ''' <summary>
    ''' get contours, watershed and fill resions
    ''' </summary>
    Public Function S_L2T1(path As String) As Mat

        Dim rand As New Random

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim cImage As Mat = Imread(path, ImreadModes.Color)

        Dim bin As New Mat
        AdaptiveThreshold(image, bin, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 5, 10)

        Dim markers As Mat = Mat.Zeros(image.Height, image.Width, DepthType.Cv8U, 1)
        Dim contours As New VectorOfVectorOfPoint
        Dim hi As Emgu.CV.IOutputArray = New Image(Of Gray, Byte)(image.Width, image.Height, New Gray(255))
        FindContours(bin, contours, hi, RetrType.List, ChainApproxMethod.ChainApproxNone)

        For i = 0 To contours.Size - 1
            Dim colour As New MCvScalar(rand.NextDouble * 255, rand.NextDouble * 255, rand.NextDouble * 255)
            DrawContours(markers, contours, i, colour, -1, 8, hi)
        Next

        Dim marker2 As New Mat
        markers.ConvertTo(marker2, DepthType.Cv32S)

        Watershed(cImage, marker2)

        Dim marker3 As New Mat
        marker2.ConvertTo(marker3, DepthType.Cv8U)
        'Imshow("result", marker3)
        'WaitKey(0)

        Return marker3
    End Function

    ''' <summary>
    ''' channel split
    ''' </summary>
    ''' <param name="image"></param>
    ''' <returns></returns>
    Public Function GetEdgeWithChannelSplit(image As Mat) As Mat
        Dim redChan As New Mat(image.Size, DepthType.Cv8U, 1)
        Dim greenChan As New Mat(image.Size, DepthType.Cv8U, 1)
        Dim blueChan As New Mat(image.Size, DepthType.Cv8U, 1)
        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim v As Byte() = image.GetRawData(j, i)
                Mat_SetPixel_1(redChan, j, i, v(2))
                Mat_SetPixel_1(greenChan, j, i, v(1))
                Mat_SetPixel_1(blueChan, j, i, v(0))
            Next
        Next
        Dim mag_r As Mat = W9.GetEdgeMagnitude(redChan)
        Dim mag_g As Mat = W9.GetEdgeMagnitude(greenChan)
        Dim mag_b As Mat = W9.GetEdgeMagnitude(blueChan)
        Dim mag As New Mat(image.Size, DepthType.Cv32F, 1)
        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim vr As Single = BitConverter.ToSingle(mag_r.GetRawData(j, i), 0)
                Dim vg As Single = BitConverter.ToSingle(mag_g.GetRawData(j, i), 0)
                Dim vb As Single = BitConverter.ToSingle(mag_b.GetRawData(j, i), 0)
                Dim max As Single = {vr, vg, vb}.Max
                Mat_SetPixel_4(mag, j, i, BitConverter.GetBytes(max))
            Next
        Next
        'Threshold(mag, mag, 0.5F, 1.0F, ThresholdType.Binary)
        Return mag
    End Function

    Public Sub EdgeIntensification(source As Mat, mask As Mat, alpha As Single)
        For j = 0 To source.Rows - 1
            For i = 0 To source.Cols - 1
                Dim pixel1 As Single = BitConverter.ToSingle(source.GetRawData(j, i), 0)
                Dim pixel2 As Single = BitConverter.ToSingle(mask.GetRawData(j, i), 0)
                pixel2 = pixel2 * alpha + 1.0 * (1.0 - alpha)
                Dim finalPixel As Single = pixel1 * pixel2
                Mat_SetPixel_4(source, j, i, BitConverter.GetBytes(finalPixel))
            Next
        Next
        Normalize(source, source, 0.0, 1.0, NormType.MinMax)
    End Sub

End Module
