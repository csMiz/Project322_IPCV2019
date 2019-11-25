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


    ''' <summary>
    ''' 使用viola-jones识别，
    ''' 步骤：1.haar特征，
    ''' 2.Adaboost，
    ''' 3.Cascade
    ''' </summary>
    Public Sub S_T1()
        S_Haar()
        S_LoadTrain()
        S_AdaBoost()
        S_AdaBoost_Strong()

        S_PrintSampleInfo()

    End Sub

    Private Sub S_Haar()

        '11x24
        '基本类型：
        '2格，3格，4格
        '最小特征为4x4

        For w = 4 To 10
            For h = 4 To 23
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

        For i = 1 To 4
            Dim tmpimg As Mat = Imread("C:\Users\sscs\Desktop\mah\train\train0" & CStr(i) & ".png", ImreadModes.Grayscale)
            Dim integ As New Mat(24, 11, DepthType.Cv32S, 1)
            Integral(tmpimg, integ)
            'Train.Add(integ)
            Dim sample As New MyImageSample
            With sample
                .IntegralImage = integ
                .Classification = 1.0F
                .Weight = 1 / 8
            End With
            SampleList.Add(sample)
            tmpimg.Dispose()
        Next
        For i = 1 To 4
            Dim tmpimg As Mat = Imread("C:\Users\sscs\Desktop\mah\train\negative0" & CStr(i) & ".png", ImreadModes.Grayscale)
            Dim integ As New Mat(24, 11, DepthType.Cv32S, 1)
            Integral(tmpimg, integ)
            'Negative.Add(integ)
            Dim sample As New MyImageSample
            With sample
                .IntegralImage = integ
                .Classification = 0.0F
                .Weight = 1 / 8
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
        If minER <= 0.5 Then Throw New Exception

        Dim result As New MyWeakOutput
        result.Feature = HaarFeatureList(minIndex)
        Dim betaT As Single = minER / (1 - minER)
        result.Weight = Math.Log10(1 / betaT)
        WeakClassifierOutput.Add(result)

        'step 3: update weight
        For Each sample As MyImageSample In SampleList
            Dim fValue As Single = sample.CalculateE(result.Feature, True)
            If fValue < 0.5 Then
                sample.Weight = sample.Weight * betaT
            End If
        Next


    End Sub

    Public Sub S_AdaBoost_Strong()

        'step 4: strong classifier
        Dim strong1 As New MyStrongOutput
        For i = 0 To 1
            strong1.WeakInput.Add(WeakClassifierOutput(i))
        Next
        strong1.SetWeight()

        StrongClassifierOutput.Add(strong1)

        For j = 3 To 25
            Dim tmpStrong As New MyStrongOutput
            For i = 0 To j - 1
                tmpStrong.WeakInput.Add(WeakClassifierOutput(i))
            Next


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
            Dim fpr As Single = fp / (fp + tn)
            'recall = tp / (tp+fn)
            Dim recall As Single = tp / (tp + fn)

        Next


    End Sub

    Public Sub S_PrintSampleInfo()
        For i = 0 To SampleList.Count - 1
            Dim sample As MyImageSample = SampleList(i)
            Debug.WriteLine(CStr(i) & ": Class=" & CInt(sample.Classification) & " Weight=" & sample.Weight)

        Next

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
        Public RectPixelCount As Integer = 0

        Public Sub SetRect()
            If HaarType = 0 Then    '2-rect
                RectPixelCount = Width * Height / 2
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



    End Class

    Private Class MyImageSample

        Public IntegralImage As Mat = Nothing
        Public Classification As Single = 0.0F
        Public Weight As Single = 0.0F

        Public Function CalculateE(haar As MyHaarLikeFeature, Optional isRaw As Boolean = False) As Single

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
                haarFeatureValue = (whiteArea - blackArea) / (255 * haar.RectPixelCount)

            End If

            If isRaw Then
                Dim rawResult As Single = Math.Abs(haarFeatureValue - Classification)
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
