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
        Dim dirMap As Mat = GetEdgeDirection(TargetImage)

        Dim imageHeight As Integer = TargetImage.Rows
        Dim imageWidth As Integer = TargetImage.Cols

        Dim houghSpace(imageWidth, imageHeight) As Single

        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Dim pixel As Single = BitConverter.ToSingle(TargetMag.GetRawData(j, i), 0)
                If pixel > 0.9F Then
                    Dim tmpDir As Single = BitConverter.ToSingle(dirMap.GetRawData(j, i), 0)

                    For Each vec As VectorOfFloat In HoughSample
                        If Math.Abs(vec(2) - tmpDir) < 0.314 Then
                            Dim x1 As Single = vec(0)
                            Dim y1 As Single = vec(1)
                            Dim invVec As New VectorOfFloat({-x1, -y1})

                            Dim x2 As Integer = i + invVec(0)
                            Dim y2 As Integer = j + invVec(1)
                            If x2 >= 0 AndAlso x2 < imageWidth AndAlso y2 >= 0 AndAlso y2 < imageHeight Then
                                houghSpace(x2, y2) += 1.0F
                            End If
                        End If

                    Next
                End If
            Next
        Next

        'get maximum
        Dim maxValue2 As Single = 0.0
        Dim tmpX As Integer, tmpY As Integer
        For j = 0 To 255
            For i = 0 To 255
                If houghSpace(i, j) > maxValue2 Then
                    maxValue2 = houghSpace(i, j)
                    tmpX = i
                    tmpY = j
                End If
            Next
        Next

        Dim source As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\darttest0.png", ImreadModes.Color)
        Resize(source, source, New Size(256, 256))
        CvInvoke.Rectangle(source, New Rectangle(tmpX - 0.5 * 128, tmpY - 0.5 * 128, 128, 128), New MCvScalar(255, 0, 0))

        Imshow("result", source)
        WaitKey(0)

    End Sub

    Public Sub S_L1T2()

        S_LoadSample()
        S_LoadTarget()

        Dim imageHeight As Integer = TargetImage.Rows
        Dim imageWidth As Integer = TargetImage.Cols
        ' (X, Y, Width, Height, Rotate): first scale then rotate
        ' 24-124 scale, 30 degree rotation
        Dim houghSpace(imageWidth, imageHeight) As Single
        Dim maxResultSpace(256, 256) As MyGeneralHoughResult

        For height = 180 To 190
            Dim scaleH As Single = height / 128.0F
            For width = 110 To 120
                Dim scaleW As Single = width / 128.0F

                'clear value
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        houghSpace(j, i) = 0.0F
                    Next
                Next

                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim pixel As Single = BitConverter.ToSingle(TargetMag.GetRawData(j, i), 0)
                        If pixel > 0.9F Then

                            For Each vec As VectorOfFloat In HoughSample
                                'Dim x1 As Single = Math.Cos(theta) * vec(0) * scaleW - Math.Sin(theta) * vec(1) * scaleH
                                'Dim y1 As Single = Math.Sin(theta) * vec(0) * scaleW + Math.Cos(theta) * vec(1) * scaleH
                                Dim x1 As Single = vec(0) * scaleW
                                Dim y1 As Single = vec(1) * scaleH
                                Dim invVec As New VectorOfFloat({-x1, -y1})

                                Dim x2 As Integer = i + invVec(0)
                                Dim y2 As Integer = j + invVec(1)
                                If x2 >= 0 AndAlso x2 < imageWidth AndAlso y2 >= 0 AndAlso y2 < imageHeight Then
                                    houghSpace(x2, y2) += 1.0F
                                End If

                            Next
                        End If
                    Next
                Next

                'getmax
                Dim maxValue As Single = 0.0F
                Dim maxArgs(2) As Integer
                For i = 0 To imageWidth - 1
                    For j = 0 To imageHeight - 1
                        If houghSpace(i, j) > maxValue Then
                            maxValue = houghSpace(i, j)
                            maxArgs(0) = i
                            maxArgs(1) = j
                        End If
                    Next
                Next
                maxResultSpace(width, height).X = maxArgs(0)
                maxResultSpace(width, height).Y = maxArgs(1)
                maxResultSpace(width, height).Value = maxValue

            Next
        Next

        'get maximum
        Dim maxValue2 As MyGeneralHoughResult
        Dim tmpW As Integer, tmpH As Integer
        For j = 0 To 255
            For i = 0 To 255
                If maxResultSpace(i, j).Value > maxValue2.Value Then
                    maxValue2 = maxResultSpace(i, j)
                    tmpW = i
                    tmpH = j
                End If
            Next
        Next

        Dim source As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\darttest.png", ImreadModes.Color)
        Resize(source, source, New Size(256, 256))
        CvInvoke.Rectangle(source, New Rectangle(maxValue2.X - 0.5 * tmpW, maxValue2.Y - 0.5 * tmpH, tmpW, tmpH), New MCvScalar(255, 0, 0))
        Debug.WriteLine(tmpW & "," & tmpH)

        Imshow("result", source)
        WaitKey(0)

    End Sub

    Public Sub S_LoadSample()

        Dim sampleImage As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(sampleImage, sampleImage, New Size(128, 128))

        'sobel
        Dim mag As Mat = W4.GetEdgeMagnitude(sampleImage)
        'Dim mag As Mat = GetEdgeMagnitudeCanny(sampleImage)

        Dim dir As Mat = GetEdgeDirection(sampleImage)
        Threshold(mag, mag, 0.75, 1.0, ThresholdType.Binary)

        Dim originX As Single = sampleImage.Cols / 2
        Dim originY As Single = sampleImage.Rows / 2

        For j = 0 To mag.Rows - 1 Step 2
            For i = 0 To mag.Cols - 1 Step 2
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                'Dim pixel As Byte = mag.GetRawData(j, i)(0)
                If pixel > 0.9F Then
                    Dim pixDir As Single = BitConverter.ToSingle(dir.GetRawData(j, i), 0)
                    Dim vec As New VectorOfFloat({i - originX, j - originY, pixDir})
                    HoughSample.Add(vec)
                End If
            Next
        Next

        SampleHeight = sampleImage.Rows
        SampleWidth = sampleImage.Cols

        Dim sampleDisplay As Mat = Mat.Zeros(128, 128, DepthType.Cv8U, 1)
        For Each tmpVec As VectorOfFloat In HoughSample
            Mat_SetPixel_1(sampleDisplay, 64 + tmpVec(1), 64 + tmpVec(0), 255)
        Next
        Imshow("sample", sampleDisplay)

    End Sub

    Public Sub S_L1T3()
        '

    End Sub

    Public Sub S_LoadTarget()

        TargetImage = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\darttest0.png", ImreadModes.Grayscale)
        Resize(TargetImage, TargetImage, New Size(256, 256))
        Dim mag As Mat = W4.GetEdgeMagnitude(TargetImage)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)
        TargetMag = mag
        Imshow("tarmag", mag)

    End Sub

    Public Sub S_DirTest()

        Dim myTargetImage As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart.bmp", ImreadModes.Grayscale)
        Dim myDir As Mat = GetEdgeDirection(myTargetImage)
        For j = 0 To myDir.Rows - 1
            For i = 0 To myDir.Cols - 1
                Dim v As Single = BitConverter.ToSingle(myDir.GetRawData(j, i), 0)
                v = v / Math.PI
                Mat_SetPixel_4(myDir, j, i, BitConverter.GetBytes(v))
            Next
        Next
        Imshow("direction", myDir)
        WaitKey(0)

    End Sub

    Public Function GetEdgeDirection(image As Mat)
        Dim resultH As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultV As New Mat(image.Size, DepthType.Cv32F, 1)
        Dim resultT As New Mat(image.Size, DepthType.Cv32F, 1)

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

                Dim phi As Single = Math.Atan2(v2, v1)
                Dim theta As Single = phi - Math.PI / 2

                Mat_SetPixel_4(resultT, j, i, BitConverter.GetBytes(theta))
            Next
        Next

        resultH.Dispose()
        resultV.Dispose()

        Return resultT

    End Function

    Public Function GetEdgeMagnitudeCanny(image As Mat) As Mat
        Dim result As New Mat
        Blur(image, image, New Size(4, 4), New Point(-1, -1))
        Canny(image, result, 8, 16)
        Return result
    End Function

    Private Structure MyGeneralHoughResult

        Public X As Single
        Public Y As Single


        Public Value As Single

    End Structure

End Module
