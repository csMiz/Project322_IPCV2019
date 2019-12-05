Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W10

    Public Sub P_EllipseHough()

        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart12.jpg"
        Dim gray As Mat = Imread(path, ImreadModes.Grayscale)
        Resize(gray, gray, New Size(0, 0), 0.25, 0.25)

        Dim mag0 As Mat = W9.GetEdgeMagnitudeCanny(gray)
        Dim mag As New Mat
        mag0.ConvertTo(mag, DepthType.Cv32F, 1.0 / 255, 0)
        Imshow("edge", mag)
        WaitKey(0)

        Dim imageHeight As Integer = mag.Rows
        Dim imageWidth As Integer = mag.Cols
        Dim houghSpaceR(imageWidth, imageHeight, 4) As MyGeneralHoughResult    'scaleX, scaleY, best 5 result

        ' X, Y ,R for rx, G for ry, B for value(not display)
        Dim houghImage As Mat = Mat.Zeros(imageHeight, imageWidth, DepthType.Cv8U, 3)

        Debug.WriteLine("Find ellipses from heigth 45 pixel to " & CStr(CInt(imageHeight / 1.5)) & " pixel")
        Debug.WriteLine("Ignore rotation")
        For rh = 45 To CInt(imageHeight / 1.5)
            Dim lb As Integer = rh / 3.0
            Dim ub As Integer = rh * 1.1
            If ub > imageWidth Then ub = imageWidth
            For rw = lb To ub
                Dim tmpHoughSpace(imageWidth - 1, imageHeight - 1) As Single    'position
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                        If pixel > 0.5F Then
                            'basic -> not even
                            For theta = 0 To 359 Step 4
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

                For k = 0 To 4
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

                    With houghSpaceR(rw, rh, k)
                        .X = tmpMaxArgs(0)
                        .Y = tmpMaxArgs(1)
                        .Value = tmpMax
                    End With

                    tmpHoughSpace(tmpMaxArgs(0), tmpMaxArgs(1)) = 0.0F
                Next

                'draw hough image
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim value As Single = tmpHoughSpace(i, j)
                        Dim targetPixelValue As Byte = houghImage.GetRawData(j, i)(0)    'blue chan
                        If value > targetPixelValue Then
                            If value > 180 Then value = 180
                            Dim ry_colour As Integer = rh * 255.0 / (imageHeight * 0.74)
                            Dim rx_colour As Integer = rw * 255.0 / (imageHeight * 0.74)
                            Mat_SetPixel_3(houghImage, j, i, {value, CByte(ry_colour), CByte(rx_colour)})
                        End If
                    Next
                Next

            Next
            Debug.WriteLine("Matching height = " & rh & " pixel ellipse...")
        Next

        Dim result As New List(Of Single())
        Dim resultCount As Integer = 0
        Dim houghMaxValue As Single = 0
        Do While resultCount < 5
            Dim rMax As New MyGeneralHoughResult
            Dim rMaxScale(2) As Integer
            For k = 0 To 4
                For j = 45 To imageHeight - 1
                    For i = 15 To imageWidth - 1
                        Dim value As Single = houghSpaceR(i, j, k).Value
                        If value > rMax.Value Then
                            rMax = houghSpaceR(i, j, k)
                            rMaxScale(0) = i
                            rMaxScale(1) = j
                            rMaxScale(2) = k
                        End If
                    Next
                Next
            Next
            If resultCount = 0 Then
                houghMaxValue = rMax.Value
            End If

            houghSpaceR(rMaxScale(0), rMaxScale(1), rMaxScale(2)).Value = 0.0F

            If rMax.Value >= 30 Then
                Dim different As Boolean = True
                For Each tmpEllipse As Single() In result
                    Dim distance As Double = Math.Sqrt((rMax.X - tmpEllipse(0)) ^ 2 + (rMax.Y - tmpEllipse(1)) ^ 2)
                    Dim r_dist As Double = Math.Sqrt((rMaxScale(0) - tmpEllipse(2)) ^ 2 + (rMaxScale(1) - tmpEllipse(3)) ^ 2)
                    If distance <= 7 AndAlso r_dist <= 7 Then    'filter same ellipses
                        different = False
                    End If
                Next
                If different Then
                    result.Add({rMax.X, rMax.Y, rMaxScale(0), rMaxScale(1)})
                    resultCount += 1
                End If
            Else
                Exit Do
            End If
        Loop

        'Return result
        Debug.WriteLine("Ellipses result: count = " & result.Count)
        If result.Count > 0 Then
            For i = 0 To result.Count - 1
                Dim tmpEll As Single() = result(i)
                Debug.WriteLine("Index " & i & ": Centre(" & tmpEll(0) & ", " & tmpEll(1) & "), Radius(" & tmpEll(2) & ", " & tmpEll(3) & ")")
            Next
        End If

        'show hough image
        If result.Count > 0 Then
            For j = 0 To imageHeight - 1
                For i = 0 To imageWidth - 1
                    Dim pixel As Byte() = houghImage.GetRawData(j, i)
                    Dim redChan As Integer = CInt(pixel(2)) * CInt(pixel(0)) / houghMaxValue
                    Dim greenChan As Integer = CInt(pixel(1)) * CInt(pixel(0)) / houghMaxValue
                    'Dim blueChan As Integer = CInt(pixel(0)) * 128.0 / houghMaxValue
                    Mat_SetPixel_3(houghImage, j, i, {0, CByte(greenChan), CByte(redChan)})
                Next
            Next
            Resize(houghImage, houghImage, New Size(0, 0), 2, 2)
            Imshow("hough image", houghImage)
            WaitKey(0)
        End If

        gray.Dispose()
        houghImage.Dispose()
        mag.Dispose()
        mag0.Dispose()

    End Sub

    Public Sub P_GHT_Sample()
        Dim sampleImage As Mat = Imread("C:\Users\sscs\Desktop\Study\CV\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(sampleImage, sampleImage, New Size(128, 128))

        Dim houghSample As New List(Of VectorOfFloat)
        Dim sampleHeight As Integer, sampleWidth As Integer

        'sobel
        Dim mag As Mat = W4.GetEdgeMagnitude(sampleImage)
        '-> canny will ignore/intensify weak edges

        Dim dir As Mat = GetEdgeDirection(sampleImage)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)

        Dim originX As Single = sampleImage.Cols / 2
        Dim originY As Single = sampleImage.Rows / 2

        For j = 0 To mag.Rows - 1 Step 2
            For i = 0 To mag.Cols - 1 Step 2
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                'Dim pixel As Byte = mag.GetRawData(j, i)(0)
                If pixel > 0.9F Then
                    Dim pixDir As Single = BitConverter.ToSingle(dir.GetRawData(j, i), 0)
                    Dim vec As New VectorOfFloat({i - originX, j - originY, pixDir})
                    houghSample.Add(vec)
                End If
            Next
        Next

        sampleHeight = sampleImage.Rows
        sampleWidth = sampleImage.Cols

        Dim sampleDisplay As Mat = Mat.Zeros(128, 128, DepthType.Cv8U, 1)
        For Each tmpVec As VectorOfFloat In houghSample
            Mat_SetPixel_1(sampleDisplay, 64 + tmpVec(1), 64 + tmpVec(0), 255)
        Next
        Resize(sampleDisplay, sampleDisplay, New Size(0, 0), 2, 2)
        Imshow("sample", sampleDisplay)
        WaitKey(0)

        sampleImage.Dispose()
        mag.Dispose()
        For Each sample As VectorOfFloat In houghSample
            sample.Dispose()
        Next
        dir.Dispose()
        sampleDisplay.Dispose()

    End Sub

    Public Sub P_LineCircleFilter()
        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart1.jpg"
        Dim gray As Mat = Imread(path, ImreadModes.Grayscale)
        Dim mag As Mat = W4.GetEdgeMagnitude(gray)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)

        Dim rawCircle As List(Of Single()) = W9.MyHoughCircle(mag)
        Dim rawLine As List(Of Single()) = W9.MyHoughLine(mag)
        Dim image1 As Mat = Imread(path, ImreadModes.Color)
        For Each tmpCircle As Single() In rawCircle
            CvInvoke.Circle(image1, New Point(tmpCircle(0), tmpCircle(1)), tmpCircle(2), New MCvScalar(255, 0, 255))
        Next
        For Each tmpLine As Single() In rawLine
            Dim dis As Single = tmpLine(0)
            Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)
            If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
                Dim p1s As New Point(Math.Abs(dis), 0)
                Dim p2s As New Point(Math.Abs(dis), gray.Rows - 1)
                Line(image1, p1s, p2s, New MCvScalar(255, 255, 0))
            Else
                Dim p1 As New Point(0, dis / Math.Cos(theta))
                Dim p2 As New Point(gray.Cols - 1, (dis / Math.Cos(theta) - gray.Cols * Math.Tan(theta)))
                Line(image1, p1, p2, New MCvScalar(255, 255, 0))
            End If
        Next

        Imshow("raw", image1)

        Dim filterCircle As List(Of Single()) = W9.CircleFilter(rawCircle)
        Dim filterLine As List(Of Single()) = W9.LineFilter(rawLine)
        Dim image2 As Mat = Imread(path, ImreadModes.Color)
        For Each tmpCircle As Single() In filterCircle
            CvInvoke.Circle(image2, New Point(tmpCircle(0), tmpCircle(1)), tmpCircle(2), New MCvScalar(255, 0, 255))
        Next
        For Each tmpLine As Single() In filterLine
            Dim dis As Single = tmpLine(0)
            Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)
            If tmpLine(1) = 90 OrElse tmpLine(1) = 270 Then
                Dim p1s As New Point(Math.Abs(dis), 0)
                Dim p2s As New Point(Math.Abs(dis), gray.Rows - 1)
                Line(image2, p1s, p2s, New MCvScalar(255, 255, 0))
            Else
                Dim p1 As New Point(0, dis / Math.Cos(theta))
                Dim p2 As New Point(gray.Cols - 1, (dis / Math.Cos(theta) - gray.Cols * Math.Tan(theta)))
                Line(image2, p1, p2, New MCvScalar(255, 255, 0))
            End If
        Next

        Imshow("filter", image2)

        WaitKey(0)

        gray.Dispose()
        mag.Dispose()
        image1.Dispose()
        image2.Dispose()

    End Sub

    Public Sub P_Quad()
        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart3.jpg"
        Dim gray As Mat = Imread(path, ImreadModes.Grayscale)
        Dim mag As Mat = W4.GetEdgeMagnitude(gray)
        Threshold(mag, mag, 0.5, 1.0, ThresholdType.Binary)
        Imshow("raw", mag)

        Dim cut As Mat() = W9.ImageSegmentation(gray, 0.5, 0.25)
        For i = 0 To 3
            Dim tmpMag As Mat = W4.GetEdgeMagnitude(cut(i))
            Threshold(tmpMag, tmpMag, 0.5, 1.0, ThresholdType.Binary)
            Dim windowName As String = "segment" & i
            Imshow(windowName, tmpMag)
        Next

        WaitKey(0)

        gray.Dispose()
        mag.Dispose()
        For i = 0 To 3
            cut(i).Dispose()
        Next

    End Sub

    Public Sub P_SlidingWindow()

        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart8.jpg"
        Dim image As Mat = Imread(path, ImreadModes.Color)
        Dim grayImg As Mat = Imread(path, ImreadModes.Grayscale)

        Dim matBuffer As New List(Of Mat)

        Dim cannyFull As Mat = W9.GetEdgeMagnitudeCanny(grayImg)
        Imshow("full", cannyFull)
        NamedWindow("sliding")

        For windowSize_i As Integer = 150 To 150 Step -50
            For windowY_i As Integer = 0 To grayImg.Rows - windowSize_i - 1 Step 25
                For windowX_i As Integer = 0 To grayImg.Cols - windowSize_i - 1 Step 25
                    Dim windowSize As Integer = windowSize_i
                    Dim windowX As Integer = windowX_i
                    Dim windowY As Integer = windowY_i

                    Dim win As Mat = W9.GetSlidingWindow(grayImg, windowX, windowY, windowSize)
                    Dim tmpCanny As Mat = W9.GetEdgeMagnitudeCanny(win)
                    matBuffer.Add(tmpCanny)
                    Imshow("sliding", tmpCanny)
                    WaitKey(500)

                    win.Dispose()
                    If matBuffer.Count >= 2 Then
                        matBuffer(0).Dispose()
                        matBuffer.RemoveAt(0)
                    End If
                Next
            Next
        Next

        WaitKey(0)

        image.Dispose()
        grayImg.Dispose()
        cannyFull.Dispose()
        For Each win As Mat In matBuffer
            win.Dispose()
        Next

    End Sub

    Public Sub P_Watershed()

        Dim rand As New Random
        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart10.jpg"

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim cImage As Mat = Imread(path, ImreadModes.Color)

        Dim bin As New Mat
        AdaptiveThreshold(image, bin, 255, AdaptiveThresholdType.GaussianC, ThresholdType.BinaryInv, 5, 10)

        Dim markers As Mat = Mat.Zeros(image.Height, image.Width, DepthType.Cv8U, 1)
        Dim contours As New VectorOfVectorOfPoint
        Dim hi As Emgu.CV.IOutputArray = New Image(Of Gray, Byte)(image.Width, image.Height, New Gray(255))
        FindContours(bin, contours, hi, RetrType.List, ChainApproxMethod.ChainApproxNone)

        For i = 0 To contours.Size - 1
            Dim colour As New MCvScalar(rand.NextDouble * 255, rand.NextDouble * 255, rand.NextDouble * 255)
            DrawContours(markers, contours, i, colour, -1, 8, hi)
        Next

        Dim marker2 As New Mat
        markers.ConvertTo(marker2, DepthType.Cv32S)

        Watershed(cImage, marker2)

        Dim marker3 As New Mat
        marker2.ConvertTo(marker3, DepthType.Cv8U)

        Dim waterSobel As Mat = W4.GetEdgeMagnitude(marker3)
        Dim oriSobel As Mat = W4.GetEdgeMagnitude(image)
        Dim proc As Mat = New Mat
        Dim p1mat As New Mat(waterSobel.Size, DepthType.Cv32F, 1)
        For j = 0 To p1mat.Rows - 1
            For i = 0 To p1mat.Cols - 1
                Mat_SetPixel_4(p1mat, j, i, BitConverter.GetBytes(0.1F))
            Next
        Next
        Add(p1mat, waterSobel, waterSobel)
        Multiply(waterSobel, oriSobel, proc)
        'Normalize(proc, proc, 0.0, 1.0, NormType.MinMax)

        Threshold(waterSobel, waterSobel, 0.5, 1.0, ThresholdType.Binary)
        Threshold(oriSobel, oriSobel, 0.5, 1.0, ThresholdType.Binary)
        Threshold(proc, proc, 0.1, 1.0, ThresholdType.Binary)

        Imshow("watershed", waterSobel)
        Imshow("original", oriSobel)
        Imshow("multiply", proc)
        WaitKey(0)

        image.Dispose()
        cImage.Dispose()
        bin.Dispose()
        markers.Dispose()
        contours.Dispose()
        hi.Dispose()
        marker2.Dispose()
        marker3.Dispose()
        waterSobel.Dispose()
        oriSobel.Dispose()
        proc.Dispose()
        p1mat.Dispose()

    End Sub

    Public Sub P_ChanSplit()
        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart10.jpg"

        Dim image As Mat = Imread(path, ImreadModes.Grayscale)
        Dim cImage As Mat = Imread(path, ImreadModes.Color)

        Dim oriSobel As Mat = W4.GetEdgeMagnitude(image)
        Threshold(oriSobel, oriSobel, 0.5, 1.0, ThresholdType.Binary)
        Imshow("original", oriSobel)

        Dim chanSplit As Mat = W7.GetEdgeWithChannelSplit(cImage)
        Threshold(chanSplit, chanSplit, 0.5, 1.0, ThresholdType.Binary)
        Imshow("channel split", chanSplit)

        WaitKey(0)

        image.Dispose()
        cImage.Dispose()
        oriSobel.Dispose()
        chanSplit.Dispose()

    End Sub

    Public Sub P_CannySobel()

        Dim path As String = "C:\Users\sscs\Desktop\Study\CV\materials\dart11.jpg"
        Dim image As Mat = Imread(path, ImreadModes.Color)
        Dim grayImg As Mat = Imread(path, ImreadModes.Grayscale)

        Dim sobel As Mat = W4.GetEdgeMagnitude(grayImg)
        Threshold(sobel, sobel, 0.5, 1.0, ThresholdType.Binary)

        Dim canny As Mat = W9.GetEdgeMagnitudeCanny(grayImg)

        Imshow("sobel", sobel)
        Imshow("canny", canny)

        WaitKey(0)
        image.Dispose()
        grayImg.Dispose()
        sobel.Dispose()
        canny.Dispose()

    End Sub

    Private Structure MyGeneralHoughResult

        Public X As Single
        Public Y As Single
        Public Value As Single

    End Structure


End Module
