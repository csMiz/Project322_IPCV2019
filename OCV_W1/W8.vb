Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util
Imports Emgu.CV.Structure

Imports System.Drawing

Module W8

    '要识别麻将牌中的“条”
    '训练样本图片大小为11x24

    Private HaarFeatureList As New List(Of MyHaarLikeFeature)
    'Private Train As New List(Of Mat)
    'Private Negative As New List(Of Mat)
    Private SampleList As New List(Of MyImageSample)
    Private WeakClassifierOutput As New List(Of MyWeakOutput)
    Private StrongClassifierOutput As New List(Of MyStrongOutput)
    Private CascadeOutput As New List(Of MyStrongOutput)
    Private DetectOutput As New List(Of Rectangle)

    Private DetectTargetSource As Mat = Nothing


    ''' <summary>
    ''' 使用viola-jones识别，
    ''' 步骤：1.haar特征，
    ''' 2.Adaboost，
    ''' 3.Cascade
    ''' </summary>
    Public Sub S_T1()
        S_Haar()
        S_LoadTrain()
        For i = 0 To 24
            S_AdaBoost()
            'S_PrintSampleInfo()
        Next
        S_AdaBoost_Strong()
        'S_ShowHaar()
        S_Cascade()
        S_ShowResult()


    End Sub

    Private Sub S_Haar()

        '11x24
        '基本类型：
        '2格，3格，4格
        '最小特征为4x4

        For w = 2 To 10
            For h = 2 To 23
                For j = 0 To 23 - h
                    For i = 0 To 10 - w
                        '2-rect
                        If w Mod 2 = 0 Then
                            Dim tmpHaar1 As New MyHaarLikeFeature
                            With tmpHaar1
                                .X = i
                                .Y = j
                                .Width = w
                                .Height = h
                                .HaarType = 0
                                .Direction = 0    '左白又黑
                                .SetRect()
                            End With
                            Dim tmpHaar2 As New MyHaarLikeFeature
                            With tmpHaar2
                                .X = i
                                .Y = j
                                .Width = w
                                .Height = h
                                .HaarType = 0
                                .Direction = 1    '左黑又白
                                .SetRect()
                            End With
                            HaarFeatureList.Add(tmpHaar1)
                            HaarFeatureList.Add(tmpHaar2)
                        End If
                        If h Mod 2 = 0 Then
                            Dim tmpHaar1 As New MyHaarLikeFeature
                            With tmpHaar1
                                .X = i
                                .Y = j
                                .Width = w
                                .Height = h
                                .HaarType = 0
                                .Direction = 2    '上白下黑
                                .SetRect()
                            End With
                            Dim tmpHaar2 As New MyHaarLikeFeature
                            With tmpHaar2
                                .X = i
                                .Y = j
                                .Width = w
                                .Height = h
                                .HaarType = 0
                                .Direction = 3    '上黑下白
                                .SetRect()
                            End With
                            HaarFeatureList.Add(tmpHaar1)
                            HaarFeatureList.Add(tmpHaar2)
                        End If
                        '3-rect

                        '4-rect

                    Next
                Next
            Next
        Next

    End Sub

    Private Sub S_LoadTrain()

        Dim imageCount As Integer = 14

        For i = 1 To 6
            Dim path As String = ""
            If i < 10 Then
                path = "C:\Users\sscs\Desktop\mah\train\train0" & CStr(i) & ".png"
            Else
                path = "C:\Users\sscs\Desktop\mah\train\train" & CStr(i) & ".png"
            End If
            Dim tmpimg As Mat = Imread(path, ImreadModes.Grayscale)
            Dim integ As New Mat(24, 11, DepthType.Cv32S, 1)
            Integral(tmpimg, integ)
            'Train.Add(integ)
            Dim sample As New MyImageSample
            With sample
                .IntegralImage = integ
                .Classification = 1.0F
                .Weight = 1 / imageCount
            End With
            SampleList.Add(sample)
            tmpimg.Dispose()
        Next
        For i = 1 To 8
            Dim path As String = ""
            If i < 10 Then
                path = "C:\Users\sscs\Desktop\mah\train\negative0" & CStr(i) & ".png"
            Else
                path = "C:\Users\sscs\Desktop\mah\train\negative" & CStr(i) & ".png"
            End If
            Dim tmpimg As Mat = Imread(path, ImreadModes.Grayscale)
            Dim integ As New Mat(24, 11, DepthType.Cv32S, 1)
            Integral(tmpimg, integ)
            'Negative.Add(integ)
            Dim sample As New MyImageSample
            With sample
                .IntegralImage = integ
                .Classification = 0.0F
                .Weight = 1 / imageCount
            End With
            SampleList.Add(sample)
            tmpimg.Dispose()
        Next


    End Sub

    Public Sub S_AdaBoost()

        'step 1: normalise weight
        Dim weightSum As Single = 0.0F
        For i = 0 To SampleList.Count - 1
            weightSum += SampleList(i).Weight
        Next
        For i = 0 To SampleList.Count - 1
            SampleList(i).Weight = SampleList(i).Weight / weightSum
        Next

        'step 2: train
        Dim erList As New List(Of Single)
        For Each haar As MyHaarLikeFeature In HaarFeatureList
            Dim errorRate As Single = 0.0F
            For Each sample As MyImageSample In SampleList
                errorRate += sample.CalculateE(haar)
            Next
            erList.Add(errorRate)
        Next

        Dim minER As Single = erList.Min
        Dim minIndex As Integer = -1
        For i = 0 To erList.Count - 1
            If erList(i) = minER Then
                minIndex = i
                Exit For
            End If
        Next
        If minER >= 0.5 Then Throw New Exception

        Dim result As New MyWeakOutput
        result.Feature = HaarFeatureList(minIndex)
        Dim betaT As Single = minER / (1 - minER)
        result.Weight = Math.Log10(1 / betaT)
        WeakClassifierOutput.Add(result)

        HaarFeatureList.RemoveAt(minIndex)

        'step 3: update weight
        For Each sample As MyImageSample In SampleList
            Dim fValue As Single = sample.CalculateE(result.Feature, 1)
            If fValue < 0.5 Then
                sample.Weight = sample.Weight * betaT
            End If
        Next


    End Sub

    Public Sub S_AdaBoost_Strong()

        'step 4: strong classifier

        For j = 2 To 25
            Dim tmpStrong As New MyStrongOutput
            For i = 0 To j - 1
                tmpStrong.WeakInput.Add(WeakClassifierOutput(i))
            Next
            tmpStrong.SetWeight()

            Dim tp As Integer = 0
            Dim fp As Integer = 0
            Dim fn As Integer = 0
            Dim tn As Integer = 0

            For Each sample As MyImageSample In SampleList
                Dim result As Integer = tmpStrong.Classify(sample)
                If result = 1 AndAlso CInt(sample.Classification) = 1 Then
                    tp += 1
                ElseIf result = 1 AndAlso CInt(sample.Classification) = 0 Then
                    fp += 1
                ElseIf result = 0 AndAlso CInt(sample.Classification) = 1 Then
                    fn += 1
                ElseIf result = 0 AndAlso CInt(sample.Classification) = 0 Then
                    tn += 1
                End If
            Next

            'false positive rate = fp / (fp+tn)
            Dim fpr As Single = fp * 1.0F / (fp + tn)
            'recall = tp / (tp+fn)
            Dim recall As Single = tp * 1.0F / (tp + fn)

            StrongClassifierOutput.Add(tmpStrong)

            Debug.Print("strong" & CStr(j) & ":recall=" & recall & " fpr=" & fpr)

        Next

        For Each j As Integer In {2, 5, 10, 12, 15, 18, 20, 22, 25}
            CascadeOutput.Add(StrongClassifierOutput(j - 2))
        Next


    End Sub

    Public Sub S_Cascade()

        Dim target As Mat = Imread("C:\Users\sscs\Desktop\mah\img4.png", ImreadModes.Grayscale)
        Dim integ As New Mat(target.Size, DepthType.Cv32S, 1)
        Integral(target, integ)

        'Sliding windows
        For zoomHeight As Integer = 24 To CInt(target.Rows / 1.5 - 1)
            Dim zoomWidth As Integer = zoomHeight * 11 / 24
            For j = 0 To target.Rows - 1 - zoomHeight
                For i = 0 To target.Cols - 1 - zoomWidth
                    Dim detect As New MyDetectWindow(i, j, zoomWidth, zoomHeight)
                    Dim result As Integer = 1
                    For k = 0 To CascadeOutput.Count - 1
                        detect.LoadStrong(CascadeOutput(k))
                        Dim classification As Integer = detect.Apply(integ)
                        If classification = 0 Then
                            result = 0
                            Exit For    'no object
                        End If
                    Next
                    If result = 1 Then    'have object
                        DetectOutput.Add(detect.GetRect)
                    End If
                Next
            Next
        Next

        target.Dispose()
        integ.Dispose()

    End Sub

    Public Sub S_ShowHaar()
        Dim sample As Mat = Imread("C:\Users\sscs\Desktop\mah\train\train01.png", ImreadModes.Color)
        For i = 0 To 24
            Dim f As MyHaarLikeFeature = WeakClassifierOutput(i).Feature
            CvInvoke.Rectangle(sample, f.GetRect1, New MCvScalar(255, 0, 0))
            CvInvoke.Rectangle(sample, f.GetRect2, New MCvScalar(255, 0, 0), -1)

            Debug.WriteLine("Feature" & CStr(i) & ":" & f.X & "," & f.Y & "," & f.Width & "," & f.Height & "," & f.Direction)
        Next

        Resize(sample, sample, New Size(110, 240))

        Imshow("haar", sample)
        WaitKey(0)

        sample.Dispose()

    End Sub

    Public Sub S_PrintSampleInfo()
        For i = 0 To SampleList.Count - 1
            Dim sample As MyImageSample = SampleList(i)
            Debug.WriteLine(CStr(i) & ": Class=" & CInt(sample.Classification) & " Weight=" & sample.Weight)

        Next

    End Sub

    Public Sub S_ShowResult()
        DetectTargetSource = Imread("C:\Users\sscs\Desktop\mah\img4.png", ImreadModes.Color)

        For Each resultRect As Rectangle In DetectOutput
            Emgu.CV.CvInvoke.Rectangle(DetectTargetSource, resultRect, New MCvScalar(0, 0, 255))
        Next

        Imshow("result", DetectTargetSource)
        WaitKey(0)


    End Sub


    Private Class MyHaarLikeFeature

        Public X As Integer
        Public Y As Integer
        Public Width As Integer
        Public Height As Integer

        Public HaarType As Integer  '0,1,2

        Public Direction As Integer  '0,1

        Public Rect1 As MyRect
        Public Rect2 As MyRect
        Public RectPixelCount As Single = 0

        Public Sub SetRect()
            If HaarType = 0 Then    '2-rect
                RectPixelCount = Width * Height / 2.0F
                If Direction = 0 Then    '左白又黑
                    Dim midX As Integer = Width / 2 + X - 1
                    Dim bottomY As Integer = Y + Height - 1
                    Dim topY As Integer = Y - 1
                    Dim leftX As Integer = X - 1
                    Dim rightX As Integer = X + Width - 1

                    Rect1 = New MyRect(leftX, topY, midX, bottomY)
                    Rect2 = New MyRect(midX, topY, rightX, bottomY)

                ElseIf Direction = 1 Then    '左黑又白
                    Dim midX As Integer = Width / 2 + X - 1
                    Dim bottomY As Integer = Y + Height - 1
                    Dim topY As Integer = Y - 1
                    Dim leftX As Integer = X - 1
                    Dim rightX As Integer = X + Width - 1

                    Rect2 = New MyRect(leftX, topY, midX, bottomY)
                    Rect1 = New MyRect(midX, topY, rightX, bottomY)

                ElseIf Direction = 2 Then    '上白下黑
                    Dim midY As Integer = Height / 2 + Y - 1
                    Dim bottomY As Integer = Y + Height - 1
                    Dim topY As Integer = Y - 1
                    Dim leftX As Integer = X - 1
                    Dim rightX As Integer = X + Width - 1

                    Rect1 = New MyRect(leftX, topY, rightX, midY)
                    Rect2 = New MyRect(leftX, midY, rightX, bottomY)

                ElseIf Direction = 3 Then
                    Dim midY As Integer = Height / 2 + Y - 1
                    Dim bottomY As Integer = Y + Height - 1
                    Dim topY As Integer = Y - 1
                    Dim leftX As Integer = X - 1
                    Dim rightX As Integer = X + Width - 1

                    Rect2 = New MyRect(leftX, topY, rightX, midY)
                    Rect1 = New MyRect(leftX, midY, rightX, bottomY)

                End If
            End If

        End Sub

        Public Function Copy() As MyHaarLikeFeature
            Dim result As New MyHaarLikeFeature
            With result
                .X = X
                .Y = Y
                .Width = Width
                .Height = Height
                .HaarType = HaarType
                .Direction = Direction
            End With
            Return result
        End Function

        Public Function GetRect1() As Rectangle
            Return New Rectangle(Rect1.X1, Rect1.Y1, Rect1.X2 - Rect1.X1, Rect1.Y2 - Rect1.Y1)
        End Function

        Public Function GetRect2() As Rectangle
            Return New Rectangle(Rect2.X1, Rect2.Y1, Rect2.X2 - Rect2.X1, Rect2.Y2 - Rect2.Y1)
        End Function

    End Class

    Private Class MyImageSample

        Public IntegralImage As Mat = Nothing
        Public Classification As Single = 0.0F
        Public Weight As Single = 0.0F

        Public Function CalculateE(haar As MyHaarLikeFeature, Optional isRaw As Integer = 0) As Single

            Dim haarFeatureValue As Single = 0.0F
            If haar.HaarType = 0 Then    '2-rect

                Dim whiteArea As Single = BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect1.Y2, haar.Rect1.X2), 0)
                If haar.Rect1.X1 >= 0 Then whiteArea -= BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect1.Y2, haar.Rect1.X1), 0)
                If haar.Rect1.Y1 >= 0 Then whiteArea -= BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect1.Y1, haar.Rect1.X2), 0)
                If haar.Rect1.X1 >= 0 AndAlso haar.Rect1.Y1 >= 0 Then whiteArea += BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect1.Y1, haar.Rect1.X1), 0)

                Dim blackArea As Single = BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect2.Y2, haar.Rect2.X2), 0)
                If haar.Rect2.X1 >= 0 Then blackArea -= BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect2.Y2, haar.Rect2.X1), 0)
                If haar.Rect2.Y1 >= 0 Then blackArea -= BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect2.Y1, haar.Rect2.X2), 0)
                If haar.Rect2.Y1 >= 0 AndAlso haar.Rect2.X1 >= 0 Then blackArea += BitConverter.ToInt32(IntegralImage.GetRawData(haar.Rect2.Y1, haar.Rect2.X1), 0)
                haarFeatureValue = (whiteArea - blackArea) / (255.0F * haar.RectPixelCount)

            End If

            If isRaw = 1 Then
                Dim rawResult As Single = Math.Abs(haarFeatureValue - Classification)
                Return rawResult
            ElseIf isRaw = 2 Then
                Dim rawResult As Single = haarFeatureValue
                Return rawResult
            End If

            Dim result As Single = Weight * Math.Abs(haarFeatureValue - Classification)
            Return result

        End Function

    End Class

    Private Class MyWeakOutput

        Public Feature As MyHaarLikeFeature = Nothing
        Public Weight As Single = 0.0F

    End Class

    Private Class MyStrongOutput

        Public WeakInput As New List(Of MyWeakOutput)
        Public WeightSum As Single = 0.0F

        Public Sub SetWeight()
            For Each weak As MyWeakOutput In WeakInput
                WeightSum += weak.Weight
            Next

        End Sub

        Public Function Classify(input As MyImageSample) As Integer
            Dim result As Single = 0.0F
            For Each weak As MyWeakOutput In WeakInput
                Dim value As Single = input.CalculateE(weak.Feature, 2)
                result += (value * weak.Weight)
            Next
            If result >= 0.5 * WeightSum Then
                Return 1
            End If
            Return 0
        End Function

        Public Function Classify(input As Mat) As Integer
            Dim sample As New MyImageSample With {
                .IntegralImage = input}
            Return Classify(sample)
        End Function

    End Class

    Private Class MyDetectWindow
        Public X As Integer
        Public Y As Integer
        Public Width As Integer
        Public Height As Integer

        Public ZoomRatioX As Single
        Public ZoomRatioY As Single

        Private CurrentStrong As MyStrongOutput = Nothing

        Public Sub New(inputX As Integer, inputY As Integer, inputW As Integer, inputH As Integer)
            X = inputX
            Y = inputY
            Width = inputW
            Height = inputH
            ZoomRatioX = Width / 11.0F
            ZoomRatioY = Height / 24.0F
        End Sub

        Public Sub LoadStrong(input As MyStrongOutput)
            Dim tmpStrong As New MyStrongOutput
            For Each weak As MyWeakOutput In input.WeakInput
                Dim weakCopy As New MyWeakOutput
                weakCopy.Feature = weak.Feature.Copy
                weakCopy.Feature.X = X + weakCopy.Feature.X * ZoomRatioX
                weakCopy.Feature.Y = Y + weakCopy.Feature.Y * ZoomRatioY
                weakCopy.Feature.Width *= ZoomRatioX
                weakCopy.Feature.Height *= ZoomRatioY
                weakCopy.Feature.SetRect()
                weakCopy.Weight = weak.Weight
                tmpStrong.WeakInput.Add(weakCopy)
            Next
            tmpStrong.WeightSum = input.WeightSum

            CurrentStrong = tmpStrong
        End Sub

        Public Function Apply(integ As Mat) As Integer
            Return CurrentStrong.Classify(integ)
        End Function

        Public Function GetRect() As Rectangle
            Return New Rectangle(X, Y, Width, Height)
        End Function

    End Class

    Private Structure MyRect
        Public X1 As Integer
        Public X2 As Integer
        Public Y1 As Integer
        Public Y2 As Integer

        Public Sub New(inputX1 As Integer, inputY1 As Integer, inputX2 As Integer, inputY2 As Integer)
            X1 = inputX1
            X2 = inputX2
            Y1 = inputY1
            Y2 = inputY2
        End Sub
    End Structure

End Module
