Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

''' <summary>
''' Week 9: further improvement on Hough transform
''' </summary>
Module W9

    ''' <summary>
    ''' process for the whole image.
    ''' </summary>
    ''' <param name="path">image path</param>
    ''' <returns></returns>
    Public Function S_L1T4_0(path As String) As List(Of Single())
        ' resize image to make it quicker to process
        Dim image As Mat = Imread(path, ImreadModes.Color)
        Resize(image, image, New Size(0, 0), 0.5, 0.5)
        Dim grayImg As Mat = Imread(path, ImreadModes.Grayscale)
        Resize(grayImg, grayImg, New Size(0, 0), 0.5, 0.5)

        Dim windowX As Integer = 0
        Dim windowY As Integer = 0
        Dim detectResult As New List(Of Single())
        ' get edge
        Dim tmpWindow As Mat = grayImg
        Dim tmpMag As Mat = W9.GetEdgeMagnitude(tmpWindow)   'sobel
        Threshold(tmpMag, tmpMag, 0.5, 1.0, ThresholdType.Binary)

        '1.circle
        Dim rawCircle As List(Of Single()) = MyHoughCircle(tmpMag)
        Dim filterCircle As List(Of Single()) = CircleFilter(rawCircle)   'merge circles
        '1.1.ellipse
        Dim rawEllipse As New List(Of Single())
        If filterCircle.Count > 0 Then
            For Each circle As Single() In filterCircle
                rawEllipse.Add({circle(0), circle(1), circle(2), circle(2)})
                'Debug.WriteLine("Circle:" & (circle(0) + windowX) & ", " & (circle(1) + windowY) & ", " & circle(2))
            Next
        Else
            rawEllipse = MyHoughEllipse(tmpMag)
            'Debug.WriteLine("ell Count:" & rawEllipse.Count)
        End If
        'give marks to circles and ellipses:
        'larger ellipse and the more looks like circle get lower mark
        'the lower mark, the better
        Dim ellMark As New List(Of Single)
        For Each tmpEll As Single() In rawEllipse
            Dim big_r As Single = tmpEll(2)
            Dim small_r As Single = tmpEll(3)
            If big_r < small_r Then
                Dim tmp As Single = big_r
                big_r = small_r
                small_r = tmp
            End If
            Dim tmpMark As Single = (big_r - small_r) + 100000 / (big_r * small_r)
            ellMark.Add(tmpMark)
            'Debug.WriteLine("Ellipse: " & (tmpEll(0) + windowX) & "," & (tmpEll(1) + windowY) & "," & tmpEll(2) & "," & tmpEll(3) & " mark=" & tmpMark)
        Next
        If rawEllipse.Count > 0 Then
            Dim minEll As Single = ellMark.Min
            Dim ellIndex As Integer = 0
            For i = 0 To ellMark.Count - 1
                If ellMark(i) = minEll Then
                    ellIndex = i
                    Exit For
                End If
            Next
            Dim tmpEll As Single() = rawEllipse(ellIndex)
            '2.concentric ellipse
            Dim concentric As Single = ParseFeature_ConcentricEllipse(tmpMag, rawEllipse(0))
            If concentric > 0 Then
                'Debug.WriteLine("concentric: " & concentric)
                '3.line
                Dim rawLine As List(Of Single()) = MyHoughLine(tmpMag)
                Dim filterLine As List(Of Single()) = LineFilter(rawLine)    'merge lines
                For Each tmpLine As Single() In filterLine
                    Dim dis As Single = tmpLine(0)
                    Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)
                Next
                'line intersection
                Dim ifIntersect As Boolean = ParseFeature_MultiLineIntersect_Verify(tmpEll(0), tmpEll(1), filterLine)
                If ifIntersect Then
                    Dim trueX As Single = windowX + tmpEll(0)
                    Dim trueY As Single = windowY + tmpEll(1)
                    detectResult.Add({trueX, trueY, tmpEll(2), tmpEll(3)})
                    'Debug.WriteLine("result found: " & trueX & ", " & trueY & ", " & tmpEll(2) & ", " & tmpEll(3))
                End If
            End If
        End If

        tmpWindow.Dispose()

        'For Each result As Single() In detectResult
        '    Debug.WriteLine("final result:" & result(0) & ", " & result(1))
        'Next

        For Each dart As Single() In detectResult
            CvInvoke.Rectangle(image, New Drawing.Rectangle(dart(0) - dart(2), dart(1) - dart(3),
                dart(2) * 2, dart(3) * 2), New MCvScalar(0, 255, 0))
        Next

        'Imwrite("C:\Users\sscs\Desktop\materials\result.png", image)
        'Imshow("win", image)
        'WaitKey(0)

        image.Dispose()

        Return detectResult

    End Function

    ''' <summary>
    ''' sliding windows applied
    ''' </summary>
    Public Async Function S_L1T4(path As String) As Task(Of List(Of Single()))

        Dim image As Mat = Imread(path, ImreadModes.Color)
        Dim grayImg As Mat = Imread(path, ImreadModes.Grayscale)

        Dim detectResult As New List(Of Single())
        'multi-thread counter
        Dim threadCount As Integer = 0

        'sliding window
        For windowSize_i As Integer = 300 To 100 Step -50
            For windowX_i As Integer = 0 To grayImg.Cols - windowSize_i - 1 Step 25
                For windowY_i As Integer = 0 To grayImg.Rows - windowSize_i - 1 Step 25

                    Dim windowSize As Integer = windowSize_i
                    Dim windowX As Integer = windowX_i
                    Dim windowY As Integer = windowY_i

                    'inner process for one thread
                    Dim process = Sub()
                                      ' maximum result allowed
                                      If detectResult.Count >= 5 Then
                                          threadCount -= 1
                                          Return
                                      End If

                                      'cutting image
                                      Dim tmpWindow As Mat = GetSlidingWindow(grayImg, windowX, windowY, windowSize)
                                      Dim tmpMag0 As Mat = W9.GetEdgeMagnitudeCanny(tmpWindow)    'use Canny instead
                                      Dim tmpMag As New Mat
                                      tmpMag0.ConvertTo(tmpMag, DepthType.Cv32F, 1.0 / 255, 0)

                                      '1.circle
                                      Dim rawCircle As List(Of Single()) = MyHoughCircle(tmpMag)
                                      Dim filterCircle As List(Of Single()) = CircleFilter(rawCircle)   'circle filter
                                      '1.1.ellipse
                                      Dim rawEllipse As New List(Of Single())
                                      If filterCircle.Count > 0 Then
                                          For Each circle As Single() In filterCircle
                                              rawEllipse.Add({circle(0), circle(1), circle(2), circle(2)})
                                          Next
                                      Else
                                          rawEllipse = MyHoughEllipse(tmpMag)
                                      End If
                                      Dim ellMark As New List(Of Single)
                                      For Each tmpEll As Single() In rawEllipse
                                          Dim big_r As Single = tmpEll(2)
                                          Dim small_r As Single = tmpEll(3)
                                          If big_r < small_r Then
                                              Dim tmp As Single = big_r
                                              big_r = small_r
                                              small_r = tmp
                                          End If
                                          Dim tmpMark As Single = (big_r - small_r) + 100000 / (big_r * small_r)
                                          ellMark.Add(tmpMark)
                                      Next
                                      If rawEllipse.Count > 0 Then
                                          Dim minEll As Single = ellMark.Min
                                          Dim ellIndex As Integer = 0
                                          For i = 0 To ellMark.Count - 1
                                              If ellMark(i) = minEll Then
                                                  ellIndex = i
                                                  Exit For
                                              End If
                                          Next
                                          Dim tmpEll As Single() = rawEllipse(ellIndex)
                                          '2.concentric circle
                                          Dim concentric As Single = ParseFeature_ConcentricEllipse(tmpMag, rawEllipse(0))
                                          If concentric > 0 Then
                                              '3.line
                                              Dim rawLine As List(Of Single()) = MyHoughLine(tmpMag)
                                              Dim filterLine As List(Of Single()) = LineFilter(rawLine)
                                              For Each tmpLine As Single() In filterLine
                                                  Dim dis As Single = tmpLine(0)
                                                  Dim theta As Single = tmpLine(1) * Math.PI / (180.0F)
                                              Next
                                              Dim ifIntersect As Boolean = ParseFeature_MultiLineIntersect_Verify(tmpEll(0), tmpEll(1), filterLine)
                                              If ifIntersect Then
                                                  Dim trueX As Single = windowX + tmpEll(0)
                                                  Dim trueY As Single = windowY + tmpEll(1)
                                                  Dim same As Boolean = False
                                                  'combine duplicate result detected
                                                  For i = detectResult.Count - 1 To 0 Step -1
                                                      Dim result As Single() = detectResult(i)
                                                      Dim distance As Single = Math.Sqrt((result(0) - trueX) ^ 2 + (result(1) - trueY) ^ 2)
                                                      If distance < 30 Then
                                                          same = True
                                                          Dim myS As Double = tmpEll(2) * tmpEll(3)
                                                          Dim targetS As Double = result(2) * result(3)
                                                          If myS > targetS Then
                                                              detectResult(i) = {trueX, trueY, tmpEll(2), tmpEll(3)}
                                                          End If
                                                      End If
                                                  Next
                                                  If Not same Then
                                                      If detectResult.Count >= 5 Then
                                                          threadCount -= 1
                                                          Return
                                                      End If
                                                      detectResult.Add({trueX, trueY, tmpEll(2), tmpEll(3)})
                                                  End If
                                              End If
                                          End If
                                      End If

                                      tmpWindow.Dispose()
                                      threadCount -= 1
                                  End Sub

                    Dim tmpTask As New Task(process)
                    '10 threads allowed at the same time
                    Do While threadCount > 10
                        Await Task.Delay(500)
                    Loop
                    threadCount += 1
                    tmpTask.Start()

                Next
            Next
        Next

EndDetect:

        Do While threadCount <> 0
            Await Task.Delay(500)
        Loop
        'For Each result As Single() In detectResult
        '    Debug.WriteLine("final result:" & result(0) & ", " & result(1))
        'Next
        'For Each dart As Single() In detectResult
        '    CvInvoke.Rectangle(image, New Drawing.Rectangle(dart(0) - dart(2), dart(1) - dart(3),
        '        dart(2) * 2, dart(3) * 2), New MCvScalar(0, 255, 0))
        'Next
        'Imwrite("C:\Users\sscs\Desktop\materials\result.png", image)

        'Imshow("win", image)
        'WaitKey(0)

        Return detectResult

    End Function

    ''' <summary>
    ''' use line-intersection and circles to define the sample. 
    ''' we intend to save these features as a file.
    ''' </summary>
    Private Sub S_LoadSample2()
        Dim grayImg As Mat = Imread("C:\Users\sscs\Desktop\materials\dart.bmp", ImreadModes.Grayscale)
        Resize(grayImg, grayImg, New Size(128, 128))
        Dim sobelMag As Mat = W9.GetEdgeMagnitude(grayImg)

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

    End Sub

    ''' <summary>
    ''' hough tranform for line
    ''' </summary>
    ''' <param name="mag">input magnitude image</param>
    ''' <returns></returns>
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

    ''' <summary>
    ''' hough transform for circle
    ''' </summary>
    ''' <param name="mag"></param>
    ''' <returns></returns>
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
        If maxHoughValue >= 70 Then
            For r = maxR - 1 To 10 Step -1
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim value As Integer = hough(i, j, r)
                        If value > maxHoughValue * 0.95 Then
                            circleList.Add({i, j, r})
                        End If
                    Next
                Next
            Next
        End If
        'Debug.WriteLine("rw:" & rw & " rh:" & rh & " value:" & tmpMax)

        Return circleList

    End Function

    ''' <summary>
    ''' hough tranform for ellipse. 
    ''' we disabled the rotation of ellipses
    ''' </summary>
    ''' <param name="mag"></param>
    ''' <returns></returns>
    Public Function MyHoughEllipse(mag As Mat) As List(Of Single())
        Dim imageHeight As Integer = mag.Rows
        Dim imageWidth As Integer = mag.Cols
        Dim houghSpaceR(imageWidth, imageHeight, 4) As MyGeneralHoughResult    'scale matrix

        For rh = 45 To CInt(imageHeight / 1.5)
            Dim lb As Integer = rh / 3.0
            Dim ub As Integer = rh * 1.1
            If ub > imageWidth Then ub = imageWidth
            For rw = lb To ub
                Dim tmpHoughSpace(imageWidth - 1, imageHeight - 1) As Single    'position matrix
                For j = 0 To imageHeight - 1
                    For i = 0 To imageWidth - 1
                        Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(j, i), 0)
                        If pixel > 0.5F Then
                            'using degree can cause uneven pixel detection
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

            Next
            If rh Mod 5 = 0 Then Debug.WriteLine("rh:" & rh)
        Next

        Dim result As New List(Of Single())
        Dim resultCount As Integer = 0
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

            houghSpaceR(rMaxScale(0), rMaxScale(1), rMaxScale(2)).Value = 0.0F

            If rMax.Value >= 30 Then
                Dim different As Boolean = True
                For Each tmpEllipse As Single() In result
                    Dim distance As Double = Math.Sqrt((rMax.X - tmpEllipse(0)) ^ 2 + (rMax.Y - tmpEllipse(1)) ^ 2)
                    Dim r_dist As Double = Math.Sqrt((rMaxScale(0) - tmpEllipse(2)) ^ 2 + (rMaxScale(1) - tmpEllipse(3)) ^ 2)
                    If distance <= 7 AndAlso r_dist <= 7 Then    'merge similar ellipses
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

    ''' <summary>
    ''' exclude lines detected from the edge magnitude. 
    ''' improve the result for further detection
    ''' </summary>
    ''' <param name="mag"></param>
    ''' <param name="lines"></param>
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

    ''' <summary>
    ''' find multi line intersection
    ''' </summary>
    ''' <param name="imageWidth"></param>
    ''' <param name="imageHeight"></param>
    ''' <param name="input">filtered lines</param>
    ''' <returns></returns>
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

    ''' <summary>
    ''' verify have intersection near a given point
    ''' </summary>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <param name="input">filtered lines</param>
    ''' <returns></returns>
    Public Function ParseFeature_MultiLineIntersect_Verify(x As Single, y As Single, input As List(Of Single())) As Boolean
        Dim cluster As New List(Of Single)
        For Each line As Single() In input
            Dim distance As Single = 0.0F
            Dim theta As Single = -line(1) * Math.PI / 180.0F
            Dim degree As Integer = line(1)
            If degree = 90 OrElse degree = 270 Then
                distance = x - line(0)
            ElseIf degree = 0 OrElse degree = 180 OrElse degree = 360 Then
                distance = y - line(0)
            Else
                'Dim k As Double = Math.Tan(theta)
                'Dim b As Double = line(0) / Math.Cos(theta)
                'Dim x1 As Double = (j - b) / k
                'distance = (x1 - i) * Math.Sin(theta)
                distance = y * Math.Cos(theta) - x * Math.Sin(theta) - line(0)
            End If
            distance = Math.Abs(distance)
            If distance < 7 Then
                cluster.Add(distance)
            End If
        Next
        If cluster.Count >= 3 Then
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' verify having 
    ''' </summary>
    ''' <param name="mag"></param>
    ''' <param name="ellipse"></param>
    ''' <returns></returns>
    Public Function ParseFeature_ConcentricEllipse(mag As Mat, ellipse As Single()) As Single
        For k = 75 To 25 Step -2
            Dim ratio As Single = k / 100.0F
            Dim rw As Single = ratio * ellipse(2)
            Dim rh As Single = ratio * ellipse(3)
            Dim vote As Single = 0.0F
            For theta = 0 To 359
                Dim angle As Single = theta * Math.PI / 180
                Dim x0 As Integer = ellipse(0) + rw * Math.Cos(angle)
                Dim y0 As Integer = ellipse(1) + rh * Math.Sin(angle)
                Dim pixel As Single = BitConverter.ToSingle(mag.GetRawData(y0, x0), 0)
                If pixel > 0.5F Then
                    vote += 1.0F
                End If
            Next
            vote /= 180.0F
            If vote > 0.5F Then
                Return ratio
            End If
        Next
        Return -1.0F
    End Function

    ''' <summary>
    ''' QuadTree cutting
    ''' </summary>
    ''' <param name="image"></param>
    ''' <param name="sepWidthRatio"></param>
    ''' <param name="sepHeightRatio"></param>
    ''' <returns></returns>
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

        'rebalance brightness
        For k = 0 To 3
            Dim subHeight As Integer = subImage(k).Rows
            Dim subWidth As Integer = subImage(k).Cols
            'can use either avg or mid of histogram
            Dim tmpHist As Single() = GetHistogram(subImage(k))
            Dim avgBright As Single = 0.0F
            For j = 0 To 255
                avgBright += (tmpHist(j) * j)
            Next
            avgBright /= (subHeight * subWidth)

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

    ''' <summary>
    ''' get sliding window image with rebalanced brightness
    ''' </summary>
    ''' <param name="image"></param>
    ''' <param name="x"></param>
    ''' <param name="y"></param>
    ''' <param name="size"></param>
    ''' <returns></returns>
    Public Function GetSlidingWindow(image As Mat, x As Integer, y As Integer, size As Integer) As Mat
        Dim rect As New Rectangle(x, y, size, size)    '100-300

        Dim tmpRegion As New Mat(image, rect)
        Dim region As Mat = New Mat
        tmpRegion.CopyTo(region)
        tmpRegion.Dispose()

        Dim avgBright As Single = 0.0F
        For i = 0 To size - 1
            For j = 0 To size - 1
                avgBright += CInt(image.GetRawData(j, i)(0))
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

    ''' <summary>
    ''' get histogram of grayscale
    ''' </summary>
    ''' <param name="image"></param>
    ''' <returns></returns>
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

    ''' <summary>
    ''' Sobel without blur
    ''' </summary>
    ''' <param name="image"></param>
    ''' <returns></returns>
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

    ''' <summary>
    ''' Canny with gaussian blur
    ''' </summary>
    ''' <param name="image"></param>
    ''' <returns></returns>
    Public Function GetEdgeMagnitudeCanny(image As Mat) As Mat
        Dim result As New Mat
        'Blur(image, image, New Size(3, 3), New Point(-1, -1))
        GaussianBlur(image, image, New Size(5, 5), 0)
        Canny(image, result, 50, 168)
        Return result
    End Function


End Module
