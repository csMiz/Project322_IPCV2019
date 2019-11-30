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

    ''' <summary>
    ''' 椭圆霍夫变换
    ''' </summary>
    Public Sub S_L1T3()
        'x, y, a, b, rotate
        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\imgs\circle1.png"
        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim mag As Mat = W4.GetEdgeMagnitude(image)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)

        Dim imageWidth As Integer = image.Cols
        Dim imageHeight As Integer = image.Rows
        Dim houghSpaceR(127, 127) As MyGeneralHoughResult

        For rw = 65 To 75
            'Debug.WriteLine("rw:" & rw)
            For rh = 25 To 35
                Dim tmpHoughSpace(imageWidth - 1, imageHeight - 1) As Single
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                        If pixel > 0.5F Then
                            '这种方法不太好，疏密不均匀
                            For theta = 0 To 359 Step 2
                                Dim angle As Single = theta * Math.PI / 180
                                Dim x0 As Integer = i + rw * Math.Cos(angle)
                                Dim y0 As Integer = j + rh * Math.Sin(angle)

                                If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                    tmpHoughSpace(x0, y0) += 1.0F
                                End If
                            Next
                        End If
                    Next
                Next

                Dim tmpMax As Single = 0.0F
                Dim tmpMaxArgs(1) As Integer
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim value As Single = tmpHoughSpace(i, j)
                        If value > tmpMax Then
                            tmpMax = value
                            tmpMaxArgs(0) = i
                            tmpMaxArgs(1) = j
                        End If
                    Next
                Next

                With houghSpaceR(rw, rh)
                    .X = tmpMaxArgs(0)
                    .Y = tmpMaxArgs(1)
                    .Value = tmpMax
                End With

            Next
        Next

        Dim rMax As MyGeneralHoughResult
        Dim rMaxScale(1) As Integer
        For j = 5 To 127
            For i = 5 To 127
                Dim value As Single = houghSpaceR(i, j).Value
                If value > rMax.Value Then
                    rMax = houghSpaceR(i, j)
                    rMaxScale(0) = i
                    rMaxScale(1) = j
                End If
            Next
        Next

        Debug.WriteLine("result:x=" & rMax.X & " y=" & rMax.Y & " rw=" & rMaxScale(0) & " rh=" & rMaxScale(1))

        Dim colorImage As Mat = Imread(path, ImreadModes.Color)
        CvInvoke.Rectangle(colorImage, New Drawing.Rectangle(rMax.X - rMaxScale(0), rMax.Y - rMaxScale(1), rMaxScale(0) * 2, rMaxScale(1) * 2),
            New MCvScalar(0, 0, 255))
        Imshow("mag", mag)
        Imshow("result", colorImage)
        WaitKey(0)

    End Sub

    ''' <summary>
    ''' 对直线和圆的霍夫变换综合分析
    ''' </summary>
    Public Sub S_L1T4()

        'Dim path As String = "C:\Users\sscs\Desktop\materials\dart1.jpg"
        Dim path As String = "C:\Users\asdfg\Desktop\ocvjtest\materials\dart10.jpg"

        Dim image As Mat = Imread(path, ImreadModes.Color)
        Dim grayImg As Mat = Imread(path, ImreadModes.Grayscale)

        'Dim sample As MyHoughSample2 = S_LoadSample2()
        Dim detectResult As New List(Of Single())

        '用sliding window检测
        For windowSize As Integer = 200 To 200 '100 To 300 Step 50
            For windowX As Integer = 800 To 800 '0 To grayImg.Cols - windowSize - 1 Step 25
                For windowY As Integer = 50 To 50 '0 To grayImg.Rows - windowSize - 1 Step 25
                    Dim tmpWindow As Mat = GetSlidingWindow(grayImg, windowX, windowY, windowSize)
                    Dim tmpMag As Mat = W4.GetEdgeMagnitude(tmpWindow)
                    Threshold(tmpMag, tmpMag, 0.5, 1.0, ThresholdType.Binary)
                    Imshow("win", tmpMag)
                    WaitKey(0)

                    tmpWindow.Dispose()
                Next
            Next
        Next


        '第一次检测
        '调整亮度
        Dim avgBright As Single = 0.0F
        For j = 0 To grayImg.Rows - 1
            For i = 0 To grayImg.Cols - 1
                avgBright = avgBright + CInt(grayImg.GetRawData(j, i)(0))
            Next
        Next
        avgBright /= (grayImg.Rows * grayImg.Cols)
        Dim ratio As Single = 128 / avgBright
        For j = 0 To grayImg.Rows - 1
            For i = 0 To grayImg.Cols - 1
                Dim pixel As Byte = grayImg.GetRawData(j, i)(0)
                Dim bright As Integer = pixel * ratio
                If bright > 255 Then bright = 255
                Mat_SetPixel_1(grayImg, j, i, bright)
            Next
        Next
        Dim sobelMag As Mat = W4.GetEdgeMagnitude(grayImg)

        '1.正圆
        Dim rawCircle As List(Of Single()) = MyHoughCircle(sobelMag)
        Dim filterCircle As List(Of Single()) = CircleFilter(rawCircle)
        If filterCircle.Count > 0 Then
            '2. 直线
            Dim rawLine As List(Of Single()) = MyHoughLine(sobelMag)
            Dim filterLine As List(Of Single()) = LineFilter(rawLine)

            For Each tmpLine As Single() In filterLine
                Dim dis As Single = tmpLine(0)
                Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)

                If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
                    Dim p1s As New Point(dis, 0)
                    Dim p2s As New Point(dis, image.Rows - 1)
                    Line(image, p1s, p2s, New MCvScalar(0, 0, 255))
                Else
                    Dim p1 As New Point(0, dis / Math.Cos(theta))
                    Dim p2 As New Point(image.Cols - 1, (dis / Math.Cos(theta) - image.Cols * Math.Tan(theta)))
                    Line(image, p1, p2, New MCvScalar(0, 0, 255))
                End If
            Next

            Dim line_intersect As Single() = ParseFeature_MultiLineIntersect(image.Width, image.Height, filterLine)

            Circle(image, New Point(line_intersect(0), line_intersect(1)), 3, New MCvScalar(255, 0, 0), -1)

            If line_intersect(0) > 0 Then
                For Each tmpCircle As Single() In filterCircle
                    Dim distance As Double = Math.Sqrt((tmpCircle(0) - line_intersect(0)) ^ 2 + (tmpCircle(1) - line_intersect(1)) ^ 2)
                    If distance < 8 Then    '是dart board
                        detectResult.Add({tmpCircle(0), tmpCircle(1), tmpCircle(2), tmpCircle(2)})
                        CvInvoke.Rectangle(image, New Rectangle(tmpCircle(0) - tmpCircle(2),
                           tmpCircle(1) - tmpCircle(2), tmpCircle(2) * 2, tmpCircle(2) * 2), New MCvScalar(0, 255, 255))
                    End If
                Next
            End If

        End If


        For Each circle As Single() In filterCircle
            CvInvoke.Circle(image, New Point(circle(0), circle(1)), circle(2), New MCvScalar(0, 255, 0))
        Next
        Imshow("source", image)
        Imwrite("C:\Users\asdfg\Desktop\ocvjtest\materials\result_401.png", image)

        WaitKey(0)

    End Sub

    ''' <summary>
    ''' 用直线和圆描述样本
    ''' </summary>
    Private Function S_LoadSample2() As MyHoughSample2
        Dim grayImg As Mat = Imread("C:\Users\sscs\Desktop\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(grayImg, grayImg, New Size(128, 128))
        Dim sobelMag As Mat = W4.GetEdgeMagnitude(grayImg)

        Threshold(sobelMag, sobelMag, 0.5, 1.0, ThresholdType.Binary)
        Imshow("dartsample", sobelMag)

        Dim rawLines As List(Of Single()) = MyHoughLine(sobelMag)
        Dim filterLines As List(Of Single()) = LineFilter(rawLines)
        'Dim rawEllipse As List(Of Single()) = MyHoughEllipse(sobelMag)
        ExcludeLines(sobelMag, filterLines)
        Dim rawCircle As List(Of Single()) = MyHoughCircle(sobelMag)

        Dim ori_image As Mat = Imread("C:\Users\sscs\Desktop\materials\dart.bmp", ImreadModes.Color)
        Resize(ori_image, ori_image, New Size(128, 128))

        For Each tmpLine As Single() In filterLines
            Dim dis As Single = tmpLine(0)
            Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)

            If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
                Dim p1s As New Point(dis, 0)
                Dim p2s As New Point(dis, ori_image.Rows - 1)
                Line(ori_image, p1s, p2s, New MCvScalar(0, 0, 255))
            Else
                Dim p1 As New Point(0, dis / Math.Cos(theta))
                Dim p2 As New Point(ori_image.Cols - 1, (dis / Math.Cos(theta) - ori_image.Cols * Math.Tan(theta)))
                Line(ori_image, p1, p2, New MCvScalar(0, 0, 255))
            End If
        Next
        For Each circle As Single() In rawCircle
            CvInvoke.Circle(ori_image, New Point(circle(0), circle(1)), circle(2), New MCvScalar(0, 255, 0))
        Next

        Dim intersect As Single() = ParseFeature_MultiLineIntersect(128, 128, filterLines)
        Circle(ori_image, New Point(intersect(0), intersect(1)), 5, New MCvScalar(255, 0, 0), -1)

        Imshow("result", ori_image)

    End Function

    Public Function MyHoughLine(mag As Mat) As List(Of Single())
        Dim diagonal As Integer = Math.Sqrt(mag.Rows ^ 2 + mag.Cols ^ 2)
        Dim houghSpace(2 * diagonal, 360) As Single
        For j = 0 To mag.Rows - 1
            For i = 0 To mag.Cols - 1
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                If pixel > 0.5 Then
                    For angle As Integer = 0 To 359
                        Dim theta As Single = angle * Math.PI / 180.0F
                        Dim distance As Single = i * Math.Sin(theta) + j * Math.Cos(theta)
                        Dim outputAngle As Integer = angle
                        distance += diagonal
                        houghSpace(distance, outputAngle) += 1.0F
                    Next
                End If
            Next
        Next

        'Dim houghImage As New Mat(360, 2 * diagonal, DepthType.Cv32F, 1)
        'For j = 0 To 359
        '    For i = 0 To 2 * diagonal - 1
        '        Mat_SetPixel_4(houghImage, j, i, BitConverter.GetBytes(houghSpace(i, j)))
        '    Next
        'Next
        'Normalize(houghImage, houghImage, 0.0, 1.0, NormType.MinMax)
        'Imshow("hough", houghImage)
        Dim maxHoughValue As Integer = 0
        For j = 0 To 359
            For i = 0 To 2 * diagonal - 1
                If houghSpace(i, j) > maxHoughValue Then
                    maxHoughValue = houghSpace(i, j)
                End If
            Next
        Next

        Dim result As New List(Of Single())
        If maxHoughValue > 50 Then
            For j = 0 To 359
                For i = 0 To 2 * diagonal - 1
                    Dim value As Single = houghSpace(i, j)
                    If value > 0.5 * maxHoughValue Then
                        result.Add({i - diagonal, j})    '{distance, theta}
                    End If
                Next
            Next
        End If
        Return result

    End Function

    Public Function MyHoughCircle(mag As Mat) As List(Of Single())
        Dim imageHeight As Integer = mag.Rows
        Dim imageWidth As Integer = mag.Cols
        Dim maxR As Integer = CInt({imageHeight, imageWidth}.Min / 2)
        Dim hough(imageWidth, imageHeight, maxR) As Single

        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                If pixel > 0.5F Then
                    For r = 35 To maxR - 1
                        For degree = 0 To 359 Step 2
                            Dim theta As Single = degree * Math.PI / 180.0F
                            Dim x0 As Integer = i + r * Math.Cos(theta)
                            Dim y0 As Integer = j + r * Math.Sin(theta)
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
                For r = 10 To maxR - 1
                    If hough(i, j, r) > maxHoughValue Then
                        maxHoughValue = hough(i, j, r)
                    End If
                Next
            Next
        Next

        Dim circleList As New List(Of Single())
        If maxHoughValue > 100 Then
            For r = maxR - 1 To 10 Step -1
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim value As Integer = hough(i, j, r)
                        If value > maxHoughValue * 0.9 Then
                            circleList.Add({i, j, r})
                        End If
                    Next
                Next
            Next
        End If
        'Debug.WriteLine("rw:" & rw & " rh:" & rh & " value:" & tmpMax)

        Return circleList

    End Function

    Public Function MyHoughEllipse(mag As Mat) As List(Of Single())
        Dim imageHeight As Integer = mag.Rows
        Dim imageWidth As Integer = mag.Cols
        Dim houghSpaceR(imageWidth, imageHeight) As MyGeneralHoughResult    '存储缩放信息

        For rh = 10 To imageHeight
            Dim lb As Integer = rh / 3.0
            Dim ub As Integer = rh * 1.1
            If ub > imageWidth Then ub = imageWidth
            For rw = lb To ub
                Dim tmpHoughSpace(imageWidth - 1, imageHeight - 1) As Single    '存储位置信息
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                        If pixel > 0.5F Then
                            '这种方法不太好，疏密不均匀
                            For theta = 0 To 359 Step 2
                                Dim angle As Single = theta * Math.PI / 180
                                Dim x0 As Integer = i + rw * Math.Cos(angle)
                                Dim y0 As Integer = j + rh * Math.Sin(angle)

                                If x0 >= 0 AndAlso x0 < imageWidth AndAlso y0 >= 0 AndAlso y0 < imageHeight Then
                                    tmpHoughSpace(x0, y0) += 1.0F
                                End If
                            Next
                        End If
                    Next
                Next

                Dim tmpMax As Single = 0.0F
                Dim tmpMaxArgs(1) As Integer
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim value As Single = tmpHoughSpace(i, j)
                        If value > tmpMax Then
                            tmpMax = value
                            tmpMaxArgs(0) = i
                            tmpMaxArgs(1) = j
                        End If
                    Next
                Next

                With houghSpaceR(rw, rh)
                    .X = tmpMaxArgs(0)
                    .Y = tmpMaxArgs(1)
                    .Value = tmpMax
                End With
                'Debug.WriteLine("rw:" & rw & " rh:" & rh & " value:" & tmpMax)
            Next
            Debug.WriteLine("rh:" & rh)
        Next

        Dim rMax As MyGeneralHoughResult
        Dim rMaxScale(1) As Integer
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Dim value As Single = houghSpaceR(i, j).Value
                If value > rMax.Value Then
                    rMax = houghSpaceR(i, j)
                    rMaxScale(0) = i
                    rMaxScale(1) = j
                End If
            Next
        Next

        Dim result As New List(Of Single())
        result.Add({rMax.X, rMax.Y, rMaxScale(0), rMaxScale(1)})
        Return result

    End Function

    Public Function LineFilter(input As List(Of Single())) As List(Of Single())
        Dim classify As New List(Of List(Of Single()))
        For Each line As Single() In input
            Dim haveClass As Boolean = False
            For Each cluster As List(Of Single()) In classify
                Dim isThisCluster As Boolean = True
                For Each compareLine As Single() In cluster
                    If Math.Abs(line(0) - compareLine(0)) > 7 OrElse Math.Abs(line(1) - compareLine(1)) > 3 Then
                        isThisCluster = False
                        Exit For
                    End If
                Next
                If isThisCluster Then
                    haveClass = True
                    cluster.Add(line)
                    Exit For
                End If
            Next
            If Not haveClass Then
                Dim newCluster As New List(Of Single())
                newCluster.Add(line)
                classify.Add(newCluster)
            End If
        Next

        Dim result As New List(Of Single())
        For Each cluster As List(Of Single()) In classify
            Dim avgDistance As Single = 0.0F
            Dim avgTheta As Single = 0.0F
            For Each tmpLine As Single() In cluster
                avgDistance += tmpLine(0)
                avgTheta += tmpLine(1)
            Next
            avgDistance /= cluster.Count
            avgTheta /= cluster.Count
            result.Add({avgDistance, avgTheta})
        Next
        Return result

    End Function

    Public Function CircleFilter(input As List(Of Single())) As List(Of Single())
        Dim classify As New List(Of List(Of Single()))
        For Each circle As Single() In input
            Dim haveClass As Boolean = False
            For Each cluster As List(Of Single()) In classify
                Dim isThisCluster As Boolean = True
                For Each compareCircle As Single() In cluster
                    Dim distance As Double = Math.Sqrt((circle(0) - compareCircle(0)) ^ 2 + (circle(1) - compareCircle(1)) ^ 2)
                    If distance > 7 OrElse Math.Abs(circle(2) - compareCircle(2)) > 5 Then
                        isThisCluster = False
                        Exit For
                    End If
                Next
                If isThisCluster Then
                    haveClass = True
                    cluster.Add(circle)
                    Exit For
                End If
            Next
            If Not haveClass Then
                Dim newCluster As New List(Of Single())
                newCluster.Add(circle)
                classify.Add(newCluster)
            End If
        Next

        Dim result As New List(Of Single())
        For Each cluster As List(Of Single()) In classify
            Dim avgX As Single = 0.0F
            Dim avgY As Single = 0.0F
            Dim avgR As Single = 0.0F
            For Each tmpCircle As Single() In cluster
                avgX += tmpCircle(0)
                avgY += tmpCircle(1)
                avgR += tmpCircle(2)
            Next
            avgX /= cluster.Count
            avgY /= cluster.Count
            avgR /= cluster.Count
            result.Add({avgX, avgY, avgR})
        Next
        Return result

    End Function

    Public Sub ExcludeLines(mag As Mat, lines As List(Of Single()))
        Dim tmpImage As New Mat
        mag.ConvertTo(tmpImage, DepthType.Cv8U, 255, 0)
        For Each tmpLine As Single() In lines
            Dim dis As Single = tmpLine(0)
            Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)
            If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
                Dim p1s As New Point(dis, 0)
                Dim p2s As New Point(dis, tmpImage.Rows - 1)
                Line(tmpImage, p1s, p2s, New MCvScalar(0), 3)
            Else
                Dim p1 As New Point(0, dis / Math.Cos(theta))
                Dim p2 As New Point(tmpImage.Cols - 1, (dis / Math.Cos(theta) - tmpImage.Cols * Math.Tan(theta)))
                Line(tmpImage, p1, p2, New MCvScalar(0), 3)
            End If
        Next
        tmpImage.ConvertTo(mag, DepthType.Cv32F, 1 / 255, 0)
        tmpImage.Dispose()
    End Sub

    Public Function ParseFeature_MultiLineIntersect(imageWidth As Integer, imageHeight As Integer, input As List(Of Single())) As Single()
        Dim intersection() As Integer = {-1, -1}
        If input.Count < 2 Then Return {-1, -1}
        Dim mergeCount As Integer = 0
        Dim leastSquare As Single = 9999.0
        For j = 0 To imageHeight - 1
            For i = 0 To imageWidth - 1
                Dim cluster As New List(Of Single)
                For Each line As Single() In input
                    Dim distance As Single = 0.0F
                    Dim theta As Single = -line(1) * Math.PI / 180.0F
                    Dim degree As Integer = line(1)
                    If degree = 90 OrElse degree = 270 Then
                        distance = i - line(0)
                    ElseIf degree = 0 OrElse degree = 180 OrElse degree = 360 Then
                        distance = j - line(0)
                    Else
                        'Dim k As Double = Math.Tan(theta)
                        'Dim b As Double = line(0) / Math.Cos(theta)
                        'Dim x1 As Double = (j - b) / k
                        'distance = (x1 - i) * Math.Sin(theta)
                        distance = j * Math.Cos(theta) - i * Math.Sin(theta) - line(0)
                    End If
                    distance = Math.Abs(distance)
                    If distance < 5 Then
                        cluster.Add(distance)
                    End If
                Next
                If cluster.Count > 0.5 * input.Count Then
                    Dim tmpMark As Single = 0.0F
                    For Each p As Single In cluster
                        tmpMark += (p ^ 2)
                    Next
                    tmpMark /= cluster.Count
                    If cluster.Count > mergeCount Then
                        mergeCount = cluster.Count
                        intersection(0) = i
                        intersection(1) = j
                        leastSquare = tmpMark
                    ElseIf cluster.Count = mergeCount Then
                        If tmpMark < leastSquare Then
                            intersection(0) = i
                            intersection(1) = j
                            leastSquare = tmpMark
                        End If
                    End If
                End If
            Next
        Next
        Return {intersection(0), intersection(1)}
    End Function

    Public Function ImageSegmentation(image As Mat, sepWidthRatio As Single, sepHeightRatio As Single) As Mat()
        Dim divWidth As Integer = image.Cols * sepWidthRatio
        Dim divHeight As Integer = image.Rows * sepHeightRatio
        Dim rects As Rectangle() = {New Rectangle(0, 0, divWidth, divHeight),
            New Rectangle(divWidth, 0, image.Cols - divWidth, divHeight),
            New Rectangle(0, divHeight, divWidth, image.Rows - divHeight),
            New Rectangle(divWidth, divHeight, image.Cols - divWidth, image.Rows - divHeight)}
        Dim subImage(3) As Mat
        For i = 0 To 3
            Dim tmpRegion As New Mat(image, rects(i))
            subImage(i) = New Mat
            tmpRegion.CopyTo(subImage(i))
            tmpRegion.Dispose()
        Next

        '调整亮度
        For k = 0 To 3
            Dim subHeight As Integer = subImage(k).Rows
            Dim subWidth As Integer = subImage(k).Cols
            Dim tmpHist As Single() = GetHistogram(subImage(k))
            Dim avgBright As Single = 0.0F
            For j = 0 To 255
                avgBright += (tmpHist(j) * j)
            Next
            avgBright /= (subHeight * subWidth)
            'Debug.WriteLine("seg" & i & ":" & avgBright)
            Dim amplify As Single = 128.0 / avgBright
            For j = 0 To subHeight - 1
                For i = 0 To subWidth - 1
                    Dim value As Integer = subImage(k).GetRawData(j, i)(0)
                    value *= amplify
                    If value > 255 Then value = 255
                    Mat_SetPixel_1(subImage(k), j, i, value)
                Next
            Next
        Next

        Return subImage
    End Function

    Public Function GetSlidingWindow(image As Mat, x As Integer, y As Integer, size As Integer) As Mat
        Dim rect As New Rectangle(x, y, size, size)    '100-300

        Dim tmpRegion As New Mat(image, rect)
        Dim region As Mat = New Mat
        tmpRegion.CopyTo(region)
        tmpRegion.Dispose()

        Dim avgBright As Single = 0.0F
        For i = 0 To size - 1
            For j = 0 To size - 1
                avgBright = avgBright + CInt(image.GetRawData(j, i)(0))
            Next
        Next
        avgBright /= (size * size)
        Dim amplify As Single = 128.0 / avgBright
        For j = 0 To size - 1
            For i = 0 To size - 1
                Dim value As Integer = region.GetRawData(j, i)(0)
                value *= amplify
                If value > 255 Then value = 255
                Mat_SetPixel_1(region, j, i, value)
            Next
        Next
        Return region
    End Function

    Public Function GetHistogram(image As Mat) As Single()
        Dim hist(255) As Single
        For i = 0 To 255
            hist(i) = 0.0F
        Next
        For j = 0 To image.Rows - 1
            For i = 0 To image.Cols - 1
                Dim pixel As Byte = image.GetRawData(j, i)(0)
                hist(pixel) += 1.0F
            Next
        Next
        Return hist
    End Function

    Public Sub S_ImageSeg()
        Dim image As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart11.jpg", ImreadModes.Grayscale)
        Dim cImage As Mat = Imread("C:\Users\asdfg\Desktop\ocvjtest\materials\dart11.jpg", ImreadModes.Color)

        'Dim mag0 As Mat = W4.GetEdgeMagnitude(image)
        'Dim mag0t As New Mat
        'Threshold(mag0, mag0t, 0.5, 1.0, ThresholdType.Binary)
        'Imshow("source", mag0t)

        Dim argW As Single = 0.5
        Dim argH As Single = 0.75

        Dim subImage As Mat() = ImageSegmentation(image, argW, argH)

        Dim mag(3) As Mat
        For i = 0 To 3
            mag(i) = W4.GetEdgeMagnitude(subImage(i))
            Normalize(mag(i), mag(i), 0.0, 1.0, NormType.MinMax)
            Threshold(mag(i), mag(i), 0.5, 1.0, ThresholdType.Binary)
        Next

        Imshow("seg1", mag(0))

        Dim circleList As New List(Of Single())
        Dim rawLines As List(Of Single())
        For i = 0 To 0
            Dim tmpCircles As List(Of Single()) = MyHoughCircle(mag(i))
            circleList.AddRange(tmpCircles)
            'rawLines = MyHoughLine(mag(i))

        Next

        For Each tmpCircle As Single() In circleList
            'Circle(cImage, New Point(tmpCircle(0) + argW * image.Cols, tmpCircle(1) + argH * image.Rows), tmpCircle(2), New MCvScalar(255, 0, 0))
            Circle(cImage, New Point(tmpCircle(0), tmpCircle(1)), tmpCircle(2), New MCvScalar(255, 0, 0))
        Next
        'For Each tmpLine As Single() In rawLines
        '    Dim dis As Single = tmpLine(0)
        '    Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)

        '    If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
        '        Dim p1s As New Point(dis, 0)
        '        Dim p2s As New Point(dis, cImage.Rows - 1)
        '        Line(cImage, p1s, p2s, New MCvScalar(0, 0, 255))
        '    Else
        '        Dim p1 As New Point(0, dis / Math.Cos(theta))
        '        Dim p2 As New Point(cImage.Cols - 1, (dis / Math.Cos(theta) - cImage.Cols * Math.Tan(theta)))
        '        Line(cImage, p1, p2, New MCvScalar(0, 0, 255))
        '    End If
        'Next

        Imshow("result", cImage)
        WaitKey(0)
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
                If theta < -Math.PI Then theta += 2 * Math.PI

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

    Public Class MyHoughSample2
        Public Lines As New List(Of Single())
        Public Circles As New List(Of Single())
    End Class

    Public Class MyMatQuadTree
        Public Node As Mat = Nothing
        Public Children(3) As Mat
    End Class

End Module
