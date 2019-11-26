Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W9

    Private HoughSample As New List(Of VectorOfFloat)
    Private SampleHeight As Single
    Private SampleWidth As Single
    Private TargetImage As Mat = Nothing
    Private TargetMag As Mat = Nothing

    ''' <summary>
    ''' general hough transform
    ''' </summary>
    Public Sub S_L1T1()

        S_LoadSample()
        S_LoadTarget()

        Dim imageHeight As Integer = TargetImage.Rows
        Dim imageWidth As Integer = TargetImage.Cols
        ' (X, Y, Width, Height, Rotate): first scale then rotate
        ' 24-124 scale, 30 degree rotation
        Dim houghSpace(imageWidth, imageHeight, 50, 50, 1) As Single

        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Dim pixel As Single = BitConverter.ToSingle(TargetMag.GetRawData(j, i), 0)
                If pixel > 0.9F Then
                    For h = 24 To 73
                        Dim scaleH As Single = h / SampleHeight
                        For w = 24 To 73
                            Dim scaleW As Single = w / SampleWidth
                            For rot = 0 To 0
                                Dim theta As Single = rot * Math.PI / 180.0F
                                For Each vec As VectorOfFloat In HoughSample
                                    Dim x1 As Single = Math.Cos(theta) * vec(0) * scaleW - Math.Sin(theta) * vec(1) * scaleH
                                    Dim y1 As Single = Math.Sin(theta) * vec(0) * scaleW + Math.Cos(theta) * vec(1) * scaleH
                                    Dim invVec As New VectorOfFloat({-x1, -y1})

                                    Dim x2 As Integer = i + invVec(0)
                                    Dim y2 As Integer = j + invVec(1)
                                    If x2 >= 0 AndAlso x2 < imageWidth AndAlso y2 >= 0 AndAlso y2 < imageHeight Then
                                        houghSpace(x2, y2, w - 24, h - 24, rot) += 1.0F
                                    End If

                                Next
                            Next
                        Next
                    Next
                End If
            Next
        Next

        'get maximum
        Dim maxValue As Single = 0.0F
        Dim maxArgs(5) As Integer
        For i = 0 To imageWidth - 1
            For j = 0 To imageHeight - 1
                For w = 24 To 73
                    For h = 24 To 73
                        For r = 0 To 0
                            If houghSpace(i, j, w - 24, h - 24, r) > maxValue Then
                                maxValue = houghSpace(i, j, w - 24, h - 24, r)
                                maxArgs(0) = i
                                maxArgs(1) = j
                                maxArgs(2) = w
                                maxArgs(3) = h
                                maxArgs(4) = r
                            End If
                        Next
                    Next
                Next
            Next
        Next

        Dim source As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart.bmp", ImreadModes.Color)
        Circle(source, New Point(maxArgs(0), maxArgs(1)), {maxArgs(2), maxArgs(3)}.Min + 24, New MCvScalar(0, 0, 255))

        Imshow("result", source)
        WaitKey(0)

    End Sub

    Private Sub S_LoadSample()

        Dim sampleImage As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(sampleImage, sampleImage, New Size(128, 128))

        'Dim blur As Image(Of Gray, Byte) = sampleImage.ToImage(Of Gray, Byte)
        'blur = blur.SmoothGaussian(23)
        'Dim blur_mat As Mat = blur.Mat

        'sobel
        Dim mag As Mat = W4.GetEdgeMagnitude(sampleImage)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)

        'Imshow("mag", mag)

        Dim originX As Single = sampleImage.Cols / 2
        Dim originY As Single = sampleImage.Rows / 2

        For j = 0 To mag.Rows - 1 Step 4
            For i = 0 To mag.Cols - 1 Step 4
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                If pixel > 0.9F Then
                    Dim vec As New VectorOfFloat({i - originX, j - originY})
                    HoughSample.Add(vec)
                End If
            Next
        Next

        SampleHeight = sampleImage.Rows
        SampleWidth = sampleImage.Cols

    End Sub

    Public Sub S_LoadTarget()

        TargetImage = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(TargetImage, TargetImage, New Size(0, 0), 0.25, 0.25)
        Dim mag As Mat = W4.GetEdgeMagnitude(TargetImage)
        Threshold(mag, mag, 0.7, 1.0, ThresholdType.Binary)
        TargetMag = mag

    End Sub

End Module
