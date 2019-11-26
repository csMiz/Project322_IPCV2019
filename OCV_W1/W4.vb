Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W4

    Public Sub S_L1S4()

        Dim threshold_h As Single = 0.5F

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "coins3.png"
        Dim bigImage As Mat = Imread(path, ImreadModes.Color)
        Dim image As New Mat(New Size(bigImage.Width / 2, bigImage.Height / 2), DepthType.Cv8U, 3)
        For j = 0 To bigImage.Rows - 1 Step 2
            For i = 0 To bigImage.Cols - 1 Step 2
                Dim v As Byte() = bigImage.GetRawData(j, i)
                Mat_SetPixel_3(image, j / 2, i / 2, v)
            Next
        Next

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
        Dim mag_r As Mat = GetEdgeMagnitude(redChan)
        Dim mag_g As Mat = GetEdgeMagnitude(greenChan)
        Dim mag_b As Mat = GetEdgeMagnitude(blueChan)
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
        Threshold(mag, mag, 0.4F, 1.0F, ThresholdType.Binary)
        'Imshow("Source", image)
        Imshow("Magnitude", mag)

        Dim imageWidth As Integer = image.Cols
        Dim imageHeight As Integer = image.Rows
        Dim maxSide As Integer = {imageWidth, imageHeight}.Max
        Dim maxR As Integer = maxSide * 0.6F

        Dim hough(imageWidth, imageHeight, maxR) As Integer
        'hough transformation
        For j = 1 To mag.Rows - 2
            For i = 1 To mag.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                If dot > 0.5 Then
                    For r = 10 To maxR - 1
                        For degree_i = 0 To 359 Step 2
                            Dim angle As Single = degree_i * Math.PI / 180.0F

                            Dim x0 As Integer = i + r * Math.Cos(angle)
                            Dim y0 As Integer = j + r * Math.Sin(angle)

                            If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                hough(x0, y0, r) += 1
                            End If

                        Next
                    Next
                End If
            Next
        Next

        Dim maxHoughValue As Integer = 0
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                For r = 1 To maxR - 1
                    If hough(i, j, r) > maxHoughValue Then
                        maxHoughValue = hough(i, j, r)
                    End If
                Next
            Next
        Next

        Dim circleList As New List(Of HoughCircle)
        For r = maxR - 1 To 10 Step -1
            For j = 0 To imageHeight - 1
                For i = 0 To imageWidth - 1
                    For Each cir As HoughCircle In circleList
                        If Math.Sqrt((i - cir.X) ^ 2 + (j - cir.Y) ^ 2) + r <= cir.R * 1.2 Then
                            GoTo lbl_nextdot
                        End If
                    Next
                    Dim value As Integer = hough(i, j, r)
                    If value > maxHoughValue * threshold_h Then
                        Dim newCircle As New HoughCircle(i, j, r)
                        circleList.Add(newCircle)
                    End If
lbl_nextdot:
                Next
            Next
        Next

        Dim finalCircleList As New List(Of HoughCircle)
        If circleList.Count > 0 Then
            finalCircleList.Add(circleList(0))
        End If
        If circleList.Count > 1 Then
            For i = 1 To circleList.Count - 1
                Dim thisCircle As HoughCircle = circleList(i)
                Dim valid As Boolean = True
                For Each target As HoughCircle In finalCircleList
                    If Math.Sqrt((thisCircle.X - target.X) ^ 2 + (thisCircle.Y - target.Y) ^ 2) + thisCircle.R <= target.R * 1.2 Then
                        valid = False
                        Exit For
                    End If
                Next
                If valid Then
                    finalCircleList.Add(thisCircle)
                End If
            Next
        End If

        For Each cir As HoughCircle In finalCircleList
            Circle(bigImage, New Point(cir.X * 2, cir.Y * 2), cir.R * 2, New MCvScalar(0, 0, 255))
        Next

        Imshow("result", bigImage)

        WaitKey(0)

        image.Dispose()
        mag.Dispose()

    End Sub

    Public Sub S_L1S3()

        Dim threshold_h As Single = 0.55F

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "coins2.png"
        Dim bigImage As Mat = Imread(path, ImreadModes.Color)
        Dim image As New Mat(New Size(bigImage.Width / 2, bigImage.Height / 2), DepthType.Cv8U, 3)
        For j = 0 To bigImage.Rows - 1 Step 2
            For i = 0 To bigImage.Cols - 1 Step 2
                Dim v As Byte() = bigImage.GetRawData(j, i)
                Mat_SetPixel_3(image, j / 2, i / 2, v)
            Next
        Next

        Dim greyImage As New Mat
        CvtColor(image, greyImage, ColorConversion.Bgr2Gray)
        Dim mag As Mat = GetEdgeMagnitude(greyImage)
        Threshold(mag, mag, 0.5, 1.0F, ThresholdType.Binary)
        'Imshow("Source", image)
        Imshow("Magnitude", mag)

        Dim imageWidth As Integer = image.Cols
        Dim imageHeight As Integer = image.Rows
        Dim maxSide As Integer = {imageWidth, imageHeight}.Max
        Dim maxR As Integer = maxSide * 0.6F

        Dim hough(imageWidth, imageHeight, maxR) As Integer
        'hough transformation
        For j = 1 To mag.Rows - 2
            For i = 1 To mag.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                If dot > 0.5 Then
                    For r = 10 To maxR - 1
                        For degree_i = 0 To 359 Step 2
                            Dim angle As Single = degree_i * Math.PI / 180.0F

                            Dim x0 As Integer = i + r * Math.Cos(angle)
                            Dim y0 As Integer = j + r * Math.Sin(angle)

                            If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                hough(x0, y0, r) += 1
                            End If

                        Next
                    Next
                End If
            Next
        Next

        Dim maxHoughValue As Integer = 0
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                For r = 1 To maxR - 1
                    If hough(i, j, r) > maxHoughValue Then
                        maxHoughValue = hough(i, j, r)
                    End If
                Next
            Next
        Next

        Dim circleList As New List(Of HoughCircle)
        For r = maxR - 1 To 10 Step -1
            For j = 0 To imageHeight - 1
                For i = 0 To imageWidth - 1
                    For Each cir As HoughCircle In circleList
                        If (i - cir.X) ^ 2 + (j - cir.Y) ^ 2 <= (cir.R) ^ 2 Then
                            GoTo lbl_nextdot
                        End If
                    Next
                    Dim value As Integer = hough(i, j, r)
                    If value > maxHoughValue * threshold_h Then
                        Dim newCircle As New HoughCircle(i, j, r)
                        circleList.Add(newCircle)
                    End If
lbl_nextdot:
                Next
            Next
        Next

        Dim finalCircleList As New List(Of HoughCircle)
        If circleList.Count > 0 Then
            finalCircleList.Add(circleList(0))
        End If
        If circleList.Count > 1 Then
            For i = 1 To circleList.Count - 1
                Dim thisCircle As HoughCircle = circleList(i)
                Dim valid As Boolean = True
                For Each target As HoughCircle In finalCircleList
                    If (thisCircle.X - target.X) ^ 2 + (thisCircle.Y - target.Y) ^ 2 < (target.R) ^ 2 Then
                        valid = False
                        Exit For
                    End If
                Next
                If valid Then
                    finalCircleList.Add(thisCircle)
                End If
            Next
        End If

        For Each cir As HoughCircle In finalCircleList
            Circle(image, cir.GetPoint, cir.R, New MCvScalar(0, 0, 255))
        Next

        Imshow("result", image)

        WaitKey(0)

        image.Dispose()
        greyImage.Dispose()
        mag.Dispose()

    End Sub

    ''' <summary>
    ''' 霍夫变换：圆
    ''' </summary>
    Public Sub S_L1S2()

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "coins1.png"
        Dim threshold_m As Single = 0.5F
        Dim threshold_h As Single = 0.4F

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim ori_image As Mat = Imread(path, ImreadModes.Color)

        Dim lightImage As Mat = New Mat
        CvtColor(ori_image, lightImage, ColorConversion.Bgr2Hsv)
        For j = 1 To image.Rows - 2
            For i = 1 To image.Cols - 2
                Dim v As Byte() = lightImage.GetRawData(j, i)
                Dim v_int As Integer = v(2)
                v_int *= 2
                If v_int > 255 Then v_int = 255
                v(2) = v_int
                Mat_SetPixel_3(lightImage, j, i, v)
            Next
        Next
        CvtColor(lightImage, lightImage, ColorConversion.Hsv2Bgr)

        Dim resultM As Mat = GetEdgeMagnitude(lightImage)
        Threshold(resultM, resultM, threshold_m, 1.0F, ThresholdType.Binary)
        Imshow("Magnitude", resultM)

        Dim imageWidth As Integer = image.Cols
        Dim imageHeight As Integer = image.Rows
        Dim maxSide As Integer = {imageWidth, imageHeight}.Max
        Dim maxR As Integer = maxSide * 0.353F

        Dim hough(imageWidth, imageHeight, maxR) As Integer
        'hough transformation
        For j = 1 To resultM.Rows - 2
            For i = 1 To resultM.Cols - 2
                Dim dot As Single = BitConverter.ToSingle(resultM.GetRawData(j, i), 0)
                If dot > threshold_m Then
                    For r = 10 To maxR - 1
                        For degree_i = 0 To 359 Step 2
                            Dim angle As Single = degree_i * Math.PI / 180.0F

                            Dim x0 As Integer = i + r * Math.Cos(angle)
                            Dim y0 As Integer = j + r * Math.Sin(angle)

                            If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                hough(x0, y0, r) += 1
                            End If

                        Next
                    Next
                End If
            Next
        Next

        Dim maxHoughValue As Integer = 0
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                For r = 1 To maxR - 1
                    If hough(i, j, r) > maxHoughValue Then
                        maxHoughValue = hough(i, j, r)
                    End If
                Next
            Next
        Next

        Dim houghImage As New Mat(image.Size, DepthType.Cv32F, 1)
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Mat_SetPixel_4(houghImage, j, i, BitConverter.GetBytes(0.0F))
            Next
        Next
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                For r = 1 To maxR - 1
                    If hough(i, j, r) > 30 Then
                        Dim getV As Single = BitConverter.ToSingle(houghImage.GetRawData(j, i), 0)
                        getV += 1.0F
                        Mat_SetPixel_4(houghImage, j, i, BitConverter.GetBytes(getV))
                    End If
                Next
            Next
        Next
        Normalize(houghImage, houghImage, 0.0, 1.0, NormType.MinMax)
        Imshow("hough", houghImage)

        Dim circleList As New List(Of KeyValuePair(Of Point, Integer))
        For r = maxR - 1 To 10 Step -1
            For j = 0 To imageHeight - 1
                For i = 0 To imageWidth - 1
                    For Each cir As KeyValuePair(Of Point, Integer) In circleList
                        If (i - cir.Key.X) ^ 2 + (j - cir.Key.Y) ^ 2 <= (cir.Value * 1.2) ^ 2 Then
                            GoTo lbl_nextdot
                        End If
                    Next
                    Dim value As Integer = hough(i, j, r)
                    If value > maxHoughValue * threshold_h Then
                        Dim kvp As New KeyValuePair(Of Point, Integer)(New Point(i, j), r)
                        circleList.Add(kvp)
                    End If
lbl_nextdot:
                Next
            Next
        Next

        Dim finalCircleList As New List(Of KeyValuePair(Of Point, Integer))
        If circleList.Count > 0 Then
            finalCircleList.Add(circleList(0))
        End If
        If circleList.Count > 1 Then
            For i = 1 To circleList.Count - 1
                Dim thisCircle As KeyValuePair(Of Point, Integer) = circleList(i)
                Dim valid As Boolean = True
                For Each target As KeyValuePair(Of Point, Integer) In finalCircleList
                    If (thisCircle.Key.X - target.Key.X) ^ 2 + (thisCircle.Key.Y - target.Key.Y) ^ 2 < (target.Value * 1.2) ^ 2 Then
                        valid = False
                        Exit For
                    End If
                Next
                If valid Then
                    finalCircleList.Add(thisCircle)
                End If
            Next
        End If

        For Each cir As KeyValuePair(Of Point, Integer) In finalCircleList
            Circle(ori_image, cir.Key, cir.Value, New MCvScalar(0, 0, 255))
        Next

        Imshow("result", ori_image)
        WaitKey(0)


        image.Dispose()
        ori_image.Dispose()
        lightImage.Dispose()
        resultM.Dispose()
        houghImage.Dispose()


    End Sub

    ''' <summary>
    ''' 霍夫变换：直线
    ''' </summary>
    Public Sub S_L1()

        Dim path As String = AppDomain.CurrentDomain.BaseDirectory & "testimg1124.png"

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim ori_image As Mat = Imread(path, ImreadModes.Color)
        Dim resultM As Mat = GetEdgeMagnitude(image)

        Dim hough As New Mat(New Size(300, 360), DepthType.Cv32F, 1)
        For i = 0 To 299
            For j = 0 To 359
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

                        'Dim alpha As Single = Math.Atan2(j, i)
                        'Dim theta As Single = m * Math.PI / (2 * 180.0F)
                        'Dim beta As Single = 0.5 * Math.PI - theta - alpha
                        'If m > 90 Then
                        '    beta = -beta
                        'End If

                        'Dim d1 As Single = Math.Sqrt(i ^ 2 + j ^ 2)
                        'Dim distance As Single = d1 * Math.Cos(beta)
                        Dim theta = m * Math.PI / 180
                        Dim distance As Single = i * Math.Sin(theta) + j * Math.Cos(theta)

                        'Dim dis_y As Integer = distance / 2.5

                        'Dim lastValue As Single = BitConverter.ToSingle(hough.GetRawData(dis_y, m), 0)
                        Dim lastValue As Single = BitConverter.ToSingle(hough.GetRawData(m, distance), 0)
                        Dim thisValue As Single = lastValue + 1.0F
                        Mat_SetPixel_4(hough, m, distance, BitConverter.GetBytes(thisValue))
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
                    Dim dis As Single = i
                    Dim theta As Single = j * Math.PI / (180.0F)

                    If j = 90 Then
                        Dim p1s As New Point(dis, 0)
                        Dim p2s As New Point(dis, image.Rows - 1)
                        Line(ori_image, p1s, p2s, New MCvScalar(0, 0, 255))
                    Else
                        Dim p1 As New Point(0, dis / Math.Cos(theta))
                        Dim p2 As New Point(image.Cols - 1, (dis / Math.Cos(theta) - image.Cols * Math.Tan(theta)))

                        Line(ori_image, p1, p2, New MCvScalar(0, 0, 255))
                    End If


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
    Public Function GetEdgeMagnitude(image As Mat) As Mat
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

    Private Class HoughCircle

        Public X As Integer

        Public Y As Integer

        Public R As Integer

        Public ReadOnly Property GetPoint
            Get
                Return New Point(X, Y)
            End Get
        End Property

        Public Sub New(inputX As Integer, inputY As Integer, inputR As Integer)
            X = inputX
            Y = inputY
            R = inputR
        End Sub

    End Class

End Module
