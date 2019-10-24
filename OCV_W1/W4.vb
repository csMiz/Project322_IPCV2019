Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W4

    ''' <summary>
    ''' 霍夫变换：圆
    ''' </summary>
    Public Sub S_L1S2()

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "coins1.png"
        Dim threshold_m As Single = 0.7F
        Dim threshold_h As Single = 0.5F

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim ori_image As Mat = Imread(path, ImreadModes.Color)
        Dim resultM As Mat = GetEdgeMagnitude(image)
        Imshow("Magnitude", resultM)

        Dim imageWidth As Integer = image.Cols
        Dim imageHeight As Integer = image.Rows
        Dim maxSide As Integer = {imageWidth, imageHeight}.Max
        Dim maxR As Integer = maxSide * 0.707F

        Dim hough(imageWidth, imageHeight, maxR) As Integer
        'hough transformation
        For j = 1 To resultM.Rows - 2
            For i = 1 To resultM.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(resultM.GetRawData(j, i), 0)
                If dot > threshold_m Then
                    For r = 1 To maxR - 1
                        For degree_i = 0 To 359
                            Dim angle As Single = degree_i * Math.PI / 180.0F

                            Dim x0 As Integer = i + r * Math.Cos(angle)
                            Dim y0 As Integer = i + r * Math.Sin(angle)

                            If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                hough(x0, y0, r) += 1
                            End If

                        Next
                    Next
                End If
            Next
        Next

        Dim hough_image As New Mat(New Size(imageWidth, imageHeight), DepthType.Cv32F, 1)
        Dim h2(imageWidth, imageHeight) As Integer
        Dim h2m As Integer = 0
        Dim h2mx As Integer, h2my As Integer
        For j = 1 To resultM.Rows - 2
            For i = 1 To resultM.Cols - 2
                Dim value As Single = 0.0F
                For r = 1 To maxR - 1
                    value += hough(i, j, r)
                Next

                h2(i, j) = value
                If value > h2m Then
                    h2m = value
                    h2mx = i
                    h2my = j
                End If

                Mat_SetPixel_4(hough_image, j, i, BitConverter.GetBytes(value))
            Next
        Next
        Normalize(hough_image, hough_image, 0.0F, 1.0F, NormType.MinMax)
        Imshow("hough", hough_image)
        WaitKey(0)


        image.Dispose()
        'ori_image.Dispose()
        resultM.Dispose()



    End Sub

    ''' <summary>
    ''' 霍夫变换：直线
    ''' </summary>
    Public Sub S_L1()

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "tri.png"

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim ori_image As Mat = Imread(path, ImreadModes.Color)
        Dim resultM As Mat = GetEdgeMagnitude(image)

        Dim hough As New Mat(New Size(360, 300), DepthType.Cv32F, 1)
        For j = 0 To 299
            For i = 0 To 359
                Mat_SetPixel_4(hough, j, i, BitConverter.GetBytes(0.0F))
            Next
        Next

        Imshow("Magnitude", resultM)

        'hough transformation
        For j = 1 To resultM.Rows - 2
            For i = 1 To resultM.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(resultM.GetRawData(j, i), 0)
                If dot > 0.5F Then
                    For m = 0 To 359

                        Dim alpha As Single = Math.Atan2(j, i)
                        Dim theta As Single = m * Math.PI / (2 * 180.0F)
                        Dim beta As Single = 0.5 * Math.PI - theta - alpha
                        If m > 90 Then
                            beta = -beta
                        End If

                        Dim d1 As Single = Math.Sqrt(i ^ 2 + j ^ 2)
                        Dim distance As Single = d1 * Math.Cos(beta)

                        Dim dis_y As Integer = distance / 2.5

                        Dim lastValue As Single = BitConverter.ToSingle(hough.GetRawData(dis_y, m), 0)
                        Dim thisValue As Single = lastValue + 1.0F
                        Mat_SetPixel_4(hough, dis_y, m, BitConverter.GetBytes(thisValue))
                    Next
                End If
            Next
        Next

        Normalize(hough, hough, 0.0F, 1.0F, NormType.MinMax)
        Imshow("Hough Plane", hough)

        For j = 1 To hough.Rows - 2
            For i = 1 To hough.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(hough.GetRawData(j, i), 0)
                If dot > 0.5F Then
                    Dim dis As Single = j * 2.5
                    Dim theta As Single = i * Math.PI / (2 * 180.0F)
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

    ''' <summary>
    ''' 通过卷积提取边缘
    ''' </summary>
    Private Function GetEdgeMagnitude(image As Mat) As Mat
        Dim resultH As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultV As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultM As New Mat(image.Size, DepthType.Cv32F, 1)

        For j = 1 To image.Rows - 2
            For i = 1 To image.Cols - 2
                Dim vh As Single
                vh = -image.GetRawData(j - 1, i - 1)(0)
                vh -= image.GetRawData(j, i - 1)(0) * 2
                vh -= image.GetRawData(j + 1, i - 1)(0)
                vh += image.GetRawData(j - 1, i + 1)(0)
                vh += image.GetRawData(j, i + 1)(0) * 2
                vh += image.GetRawData(j + 1, i + 1)(0)
                vh /= 8.0F

                Mat_SetPixel_4(resultH, j, i, BitConverter.GetBytes(CSng(vh / 255.0F)))

                Dim vv As Single
                vv = -image.GetRawData(j - 1, i - 1)(0)
                vv -= image.GetRawData(j - 1, i)(0) * 2
                vv -= image.GetRawData(j - 1, i + 1)(0)
                vv += image.GetRawData(j + 1, i - 1)(0)
                vv += image.GetRawData(j + 1, i)(0) * 2
                vv += image.GetRawData(j + 1, i + 1)(0)
                vv /= 8.0F

                Mat_SetPixel_4(resultV, j, i, BitConverter.GetBytes(CSng(vv / 255.0F)))
            Next
        Next

        For j = 1 To image.Rows - 2
            For i = 1 To image.Cols - 2
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
