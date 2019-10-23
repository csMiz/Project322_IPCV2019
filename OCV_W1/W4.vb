﻿Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W4

    ''' <summary>
    ''' 通过卷积提取边缘
    ''' </summary>
    Public Sub S_L1()

        Dim image As Mat = Imread(AppDomain.CurrentDomain.BaseDirectory & "mandrillRGB.jpg", ImreadModes.Grayscale)
        Dim resultM As Mat = GetEdgeMagnitude(image)
        Dim hough As New Mat(New Size(180, 724), DepthType.Cv8U, 1)
        For j = 0 To 723
            For i = 0 To 179
                Mat_SetPixel_1(hough, j, i, 0)
            Next
        Next

        'hough transformation
        For j = 250 To 250 'resultM.Cols - 2
            For i = 250 To 250 ' resultM.Rows - 2
                Dim dot As Single = BitConverter.ToSingle(resultM.GetRawData(j, i), 0)
                For m = 0 To 179
                    Dim theta As Single = (m - 90.0F) * Math.PI / 180.0F
                    Dim k1 As Single = Math.Tan(m * Math.PI / 180.0F)
                    Dim b1 As Single = j - i * k1
                    Dim k2 As Single = -1 / k1
                    Dim rx As Single = (-b1) / (k1 - k2)
                    Dim distance As Single = Math.Abs(rx / Math.Cos(theta))

                    Mat_SetPixel_1(hough, distance, m, 255)
                Next
            Next
        Next

        Imshow("Hough Plane", hough)
        WaitKey(0)



    End Sub

    Private Function GetEdgeMagnitude(image As Mat) As Mat
        Dim resultH As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultV As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultM As New Mat(image.Size, DepthType.Cv32F, 1)

        For j = 1 To image.Cols - 2
            For i = 1 To image.Rows - 2
                Dim vh As Integer
                vh = -image.GetRawData(j - 1, i - 1)(0)
                vh -= image.GetRawData(j, i - 1)(0) * 2
                vh -= image.GetRawData(j + 1, i - 1)(0)
                vh += image.GetRawData(j - 1, i + 1)(0)
                vh += image.GetRawData(j, i + 1)(0) * 2
                vh += image.GetRawData(j + 1, i + 1)(0)

                Mat_SetPixel_4(resultH, j, i, BitConverter.GetBytes(CSng(vh / 255.0F)))

                Dim vv As Integer
                vv = -image.GetRawData(j - 1, i - 1)(0)
                vv -= image.GetRawData(j - 1, i)(0) * 2
                vv -= image.GetRawData(j - 1, i + 1)(0)
                vv += image.GetRawData(j + 1, i - 1)(0)
                vv += image.GetRawData(j + 1, i)(0) * 2
                vv += image.GetRawData(j + 1, i + 1)(0)

                Mat_SetPixel_4(resultV, j, i, BitConverter.GetBytes(CSng(vv / 255.0F)))
            Next
        Next

        For j = 1 To image.Cols - 2
            For i = 1 To image.Rows - 2
                Dim v1 As Single = BitConverter.ToSingle(resultH.GetRawData(j, i), 0)
                Dim v2 As Single = BitConverter.ToSingle(resultV.GetRawData(j, i), 0)
                Dim v As Single = Math.Sqrt(v1 ^ 2 + v2 ^ 2)

                Mat_SetPixel_4(resultM, j, i, BitConverter.GetBytes(CSng(v)))
            Next
        Next
        Normalize(resultH, resultH, 0.0F, 1.0F, NormType.MinMax)
        Normalize(resultV, resultV, 0.0F, 1.0F, NormType.MinMax)
        Normalize(resultM, resultM, 0.0F, 1.0F, NormType.MinMax)

        resultH.Dispose()
        resultV.Dispose()

        Return resultM

    End Function

End Module