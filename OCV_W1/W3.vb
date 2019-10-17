Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W3

    ''' <summary>
    ''' 通过两倍原图减去模糊实现锐化
    ''' </summary>
    Public Sub S_L2T2()
        Dim image As Mat = Imread(AppDomain.CurrentDomain.BaseDirectory & "car1.png", ImreadModes.Grayscale)
        Dim result As New Mat(image.Size, DepthType.Cv8U, 1)

        Dim blur As Image(Of Gray, Byte) = image.ToImage(Of Gray, Byte)
        blur = blur.SmoothGaussian(23)
        Dim blur_mat As Mat = blur.Mat

        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim vint As Integer = 2 * image.GetRawData(j, i)(0) - blur_mat.GetRawData(j, i)(0)
                Dim v As Byte
                If vint > 255 Then
                    v = 255
                ElseIf vint < 0 Then
                    v = 0
                Else
                    v = CByte(vint)
                End If
                Mat_SetPixel_1(result, j, i, v)
            Next
        Next

        Imshow("source", image)
        Imshow("sharpen", result)
        WaitKey(0)

        image.Dispose()
        result.Dispose()
        blur.Dispose()
        blur_mat.Dispose()

    End Sub

    ''' <summary>
    ''' 通过中值卷积去噪
    ''' </summary>
    Public Sub S_L2T1()

        Dim image As Mat = Imread(AppDomain.CurrentDomain.BaseDirectory & "car2.png", ImreadModes.Grayscale)

        Dim result_3 As Mat = conv_med(image, 3)
        Dim result_5 As Mat = conv_med(image, 5)
        Dim result_7 As Mat = conv_med(image, 7)

        Imshow("source", image)
        Imshow("med_3", result_3)
        Imshow("med_5", result_5)
        Imshow("med_7", result_7)
        WaitKey(0)

        image.Dispose()
        result_3.Dispose()
        result_5.Dispose()
        result_7.Dispose()

    End Sub

    Private Function conv_med(image As Mat, size As Integer) As Mat

        Dim margin As Integer = (size - 1) / 2
        Dim result As New Mat(image.Size, DepthType.Cv8U, 1)
        For j = margin To image.Rows - 1 - margin
            For i = margin To image.Cols - 1 - margin
                Dim v As Byte
                Dim values As New List(Of Byte)
                For m = -margin To margin
                    For n = -margin To margin
                        values.Add(image.GetRawData(j + m, i + n)(0))
                    Next
                Next
                values.Sort()
                v = values(CInt((size * size - 1) / 2))
                Mat_SetPixel_1(result, j, i, v)
            Next
        Next

        Return result


    End Function


    ''' <summary>
    ''' 探究傅里叶变换
    ''' </summary>
    Public Sub S_L1T1()

        'generate sin-image
        Dim image As New Mat(512, 512, DepthType.Cv32F, 1)
        For j = 0 To 511
            For i = 0 To 511
                Mat_SetPixel_4(image, j, i, BitConverter.GetBytes(CSng(Math.Sin((i / 20.0) + j / 70) * 0.5F + 0.5F)))
            Next
        Next

        'apply dft
        Dim dft As Mat = GetImageFourierTransformation(image)

        Imshow("source", image)
        Imshow("dft", dft)
        WaitKey(0)

        image.Dispose()
        dft.Dispose()

    End Sub


End Module
