Imports Emgu.CV
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

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "tri.png"

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim resultM As Mat = GetEdgeMagnitude(image)
        Dim hough As New Mat(New Size(180, 145), DepthType.Cv8U, 1)
        For j = 0 To 144
            For i = 0 To 179
                Mat_SetPixel_1(hough, j, i, 0)
            Next
        Next

        'hough transformation
        For j = 1 To resultM.Rows - 2
            For i = 1 To resultM.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(resultM.GetRawData(j, i), 0)
                If dot > 0.5F Then
                    For m = 1 To 179
                        If m = 90 Then Continue For

                        Dim theta As Single = (m - 90.0F) * Math.PI / 180.0F
                        Dim k1 As Single = Math.Tan(m * Math.PI / 180.0F)
                        Dim b1 As Single = j - i * k1
                        Dim k2 As Single = -1 / k1
                        Dim rx As Single = (-b1) / (k1 - k2)
                        Dim distance As Single = Math.Abs(rx / Math.Cos(theta))
                        Dim dis_y As Integer = distance / 5

                        Dim lastValue As Integer = hough.GetRawData(dis_y, m)(0)
                        Dim thisValue As Integer = lastValue + 1
                        If thisValue > 255 Then thisValue = 255
                        Mat_SetPixel_1(hough, dis_y, m, CByte(thisValue))
                    Next
                End If
            Next
        Next

        Imshow("mag", resultM)
        Imshow("Hough Plane", hough)

        Dim ori_image As Mat = Imread(path, ImreadModes.Color)

        For j = 1 To hough.Rows - 2
            For i = 1 To hough.Cols - 2
                If i = 90 Then Continue For
                Dim dot As Byte = hough.GetRawData(j, i)(0)
                If dot > 200 Then
                    Dim dis As Single = j * 5
                    Dim theta As Single = (180 - i) * Math.PI / 180
                    Dim p1 As New Point(0, dis / Math.Cos(theta))
                    Dim p2 As New Point(image.Cols - 1, (dis / Math.Cos(theta) - image.Cols * Math.Tan(theta)))

                    Line(ori_image, p1, p2, New MCvScalar(0, 0, 255))
                End If
            Next
        Next

        Imshow("image", ori_image)
        WaitKey(0)

        image.Dispose()
        resultM.Dispose()
        hough.Dispose()
        ori_image.Dispose()

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
