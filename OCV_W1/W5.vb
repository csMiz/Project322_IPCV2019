﻿Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing


Module W5

    Public Sub S_L1T1()

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "house.png"
        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim histogram(255) As Single
        Dim hist_image As New Mat(256, 256, DepthType.Cv8U, 1)

        Imshow("source", image)

        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim value As Byte = image.GetRawData(j, i)(0)
                histogram(value) += 1.0F
            Next
        Next

        Dim hist_max As Single = histogram.Max
        For i = 0 To 255
            histogram(i) = histogram(i) / hist_max
        Next

        For j = 0 To 255
            For i = 0 To 255
                If 255 - j < histogram(i) * 255 Then
                    Mat_SetPixel_1(hist_image, j, i, 0)
                Else
                    Mat_SetPixel_1(hist_image, j, i, 255)
                End If
            Next
        Next

        Dim tmpThreshold As Single = 128.0F
        Do
            Dim sum_g1 As Single = 0.0F
            Dim sum_g2 As Single = 0.0F
            Dim count_g1 As Single = 0.0F
            Dim count_g2 As Single = 0.0F
            For j = 0 To 255
                If j < tmpThreshold Then
                    sum_g1 += (j * histogram(j))
                    count_g1 += histogram(j)
                Else
                    sum_g2 += (j * histogram(j))
                    count_g2 += histogram(j)
                End If
            Next
            Dim avg_g1 As Single = sum_g1 / count_g1
            Dim avg_g2 As Single = sum_g2 / count_g2

            Dim newThreshold = (avg_g1 + avg_g2) / 2
            If CInt(newThreshold) = CInt(tmpThreshold) Then Exit Do

            tmpThreshold = newThreshold
        Loop
        Line(hist_image, New Point(tmpThreshold, 0), New Point(tmpThreshold, 255), New MCvScalar(180))
        Imshow("histogram", hist_image)

        Dim threshold As Integer = tmpThreshold
        Dim c_image As Mat = Imread(path, ImreadModes.Color)
        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim v As Byte = image.GetRawData(j, i)(0)
                Dim source_color As Byte() = c_image.GetRawData(j, i)
                Dim mask_color As Byte()
                If v < threshold Then
                    mask_color = {0, 0, 128}
                Else
                    mask_color = {128, 0, 0}
                End If
                Dim final_color As Byte() = {0, 0, 0}
                For m = 0 To 2
                    final_color(m) = source_color(m) + (255 - source_color(m)) * (mask_color(m) / 255.0F)
                Next
                Mat_SetPixel_3(c_image, j, i, final_color)
            Next
        Next

        Imshow("c_image", c_image)

        WaitKey(0)

        image.Dispose()
        hist_image.Dispose()
        c_image.Dispose()

    End Sub


End Module
