Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum

Imports System.Drawing

Module Surface_W2

    Public Sub S_L2T3()
        Dim image As New Mat(AppDomain.CurrentDomain.BaseDirectory & "mandrill3.jpg", ImreadModes.Color)
        Dim result As New Mat

        CvtColor(image, result, ColorConversion.Bgr2Hsv)

        For y = 0 To image.Rows - 1
            For x = 0 To image.Cols - 1
                Dim getColor As Byte() = result.GetRawData(y, x)
                Dim color As Byte() = {getColor(0), getColor(1), getColor(2)}
                Dim tmpv As Integer = color(0)
                If tmpv < 40 Then
                    tmpv += 255
                End If
                tmpv -= 40
                'If tmpv > 60 Then
                '    tmpv -= 60
                'End If
                color(0) = tmpv
                Mat_SetPixel_3(result, y, x, color)
            Next
        Next

        CvtColor(result, result, ColorConversion.Hsv2Bgr)

        Imshow("source", image)
        Imshow("result", result)

        WaitKey(0)
        image.Dispose()
        result.Dispose()
    End Sub

    Public Sub S_L2T2()
        Dim image As New Mat(AppDomain.CurrentDomain.BaseDirectory & "mandrill2.jpg", ImreadModes.Color)
        Dim result As New Mat(image.Size, DepthType.Cv8U, 3)

        For y = 0 To image.Rows - 1
            For x = 0 To image.Cols - 1
                Dim getColor As Byte() = image.GetRawData(y, x)
                Dim color As Byte() = {255 - getColor(0), 255 - getColor(1), 255 - getColor(2)}  'bgr
                Mat_SetPixel_3(result, y, x, color)
            Next
        Next

        Imshow("source", image)
        Imshow("result", result)

        WaitKey(0)
        image.Dispose()
        result.Dispose()
    End Sub

    Public Sub S_L2T1()
        Dim image As New Mat(AppDomain.CurrentDomain.BaseDirectory & "mandrill0.jpg", ImreadModes.Color)
        Dim result As New Mat(image.Size, DepthType.Cv8U, 3)

        For y = 0 To image.Rows - 1
            For x = 0 To image.Cols - 1
                Dim getColor As Byte() = image.GetRawData(y, x)
                Dim color As Byte() = {getColor(2), getColor(0), getColor(1)}  'bgr
                Mat_SetPixel_3(result, y, x, color)
            Next
        Next

        Imshow("source", image)
        Imshow("result", result)

        WaitKey(0)
        image.Dispose()
        result.Dispose()
    End Sub

    Public Sub S_L1()
        Dim image As New Mat(AppDomain.CurrentDomain.BaseDirectory & "mandrillRGB.jpg", ImreadModes.Color)
        Dim lowpass As New Mat(New Size(image.Size.Width - 1, image.Size.Height - 1), DepthType.Cv8U, 3)
        Dim highpass As New Mat(New Size(image.Size.Width - 1, image.Size.Height - 1), DepthType.Cv8U, 3)
        Dim sharpen As New Mat(New Size(image.Size.Width - 1, image.Size.Height - 1), DepthType.Cv8U, 3)

        'low pass
        For y = 1 To image.Rows - 2
            For x = 1 To image.Cols - 2
                Dim bgr(2) As Byte
                For i = 0 To 2
                    Dim tmpValue As Double = 0
                    For m = -1 To 1
                        For n = -1 To 1
                            tmpValue += image.GetRawData(y + m, x + n)(i) / 9.0F
                        Next
                    Next
                    bgr(i) = CByte(tmpValue)
                Next
                Mat_SetPixel_3(lowpass, y, x, bgr)
            Next
        Next
        'high pass
        For y = 1 To image.Rows - 2
            For x = 1 To image.Cols - 2
                Dim bgr(2) As Byte
                For i = 0 To 2
                    Dim tmpValue As Double = 0
                    For m = -1 To 1
                        For n = -1 To 1
                            If m = 0 AndAlso n = 0 Then
                                tmpValue += image.GetRawData(y + m, x + n)(i) * 9.0
                            Else
                                tmpValue -= image.GetRawData(y + m, x + n)(i)
                            End If
                        Next
                    Next
                    If tmpValue < 0 Then
                        bgr(i) = 0
                    ElseIf tmpValue > 255 Then
                        bgr(i) = 255
                    Else
                        bgr(i) = CByte(tmpValue)
                    End If
                Next
                Mat_SetPixel_3(highpass, y, x, bgr)
            Next
        Next
        'sharpen
        For y = 1 To image.Rows - 2
            For x = 1 To image.Cols - 2
                Dim bgr(2) As Byte
                For i = 0 To 2
                    Dim tmpValue As Double
                    tmpValue = 2.0F * image.GetRawData(y, x)(i) - lowpass.GetRawData(y, x)(i)
                    If tmpValue < 0 Then
                        bgr(i) = 0
                    ElseIf tmpValue > 255 Then
                        bgr(i) = 255
                    Else
                        bgr(i) = CByte(tmpValue)
                    End If
                Next
                Mat_SetPixel_3(sharpen, y, x, bgr)
            Next
        Next

        Imshow("source", image)
        Imshow("low pass", lowpass)
        Imshow("high pass", highpass)
        Imshow("sharpen", sharpen)

        WaitKey(0)
        image.Dispose()
        lowpass.Dispose()
        highpass.Dispose()
        sharpen.Dispose()

    End Sub

    Public Sub S_Main1()
        Dim img As Mat = New Mat(New Size(256, 256), CvEnum.DepthType.Cv8U, 3)

        PutText(img, "Hello Frank", New Drawing.Point(70, 70), CvEnum.FontFace.HersheyComplexSmall, 0.8,
                    New [Structure].MCvScalar(255, 255, 255), 1, CvEnum.LineType.AntiAlias)

        Line(img, New Drawing.Point(74, 90), New Point(190, 90), New [Structure].MCvScalar(255, 0, 0), 2)

        Ellipse(img, New Point(130, 180), New Size(25, 25), 180, 180, 360, New [Structure].MCvScalar(0, 255, 0), 2)
        Circle(img, New Point(130, 180), 50, New [Structure].MCvScalar(0, 255, 0), 2)
        Circle(img, New Point(110, 160), 5, New [Structure].MCvScalar(0, 255, 0), 2)
        Circle(img, New Point(150, 160), 5, New [Structure].MCvScalar(0, 255, 0), 2)



        Imwrite("myimage.jpg", img)
        Imshow("title", img)
        WaitKey(0)

        img.Dispose()

        Return
    End Sub

    <Obsolete("use OCV_Helper -> mat_setPixel_3")>
    Public Sub S_SetValue(ByRef mat As Mat, row As Integer, col As Integer, value As Byte())

        Runtime.InteropServices.Marshal.Copy _
            (value, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 3)

    End Sub

End Module
