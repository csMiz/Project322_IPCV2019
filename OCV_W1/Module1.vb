Imports Emgu.CV
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Util

Module Module1

    Sub Main(ByVal args As String())

        'Dim path As String = args(0)
        'Dim result1 As List(Of Single()) = W9.S_L1T4_0(path)
        'If result1.Count = 0 Then
        '    Dim result2 As Task(Of List(Of Single())) = W9.S_L1T4(path)
        '    result1 = result2.Result
        'End If

        'Dim image As Mat = Imread(path, ImreadModes.Color)
        'For Each dart As Single() In result1
        '    CvInvoke.Rectangle(image, New Drawing.Rectangle(dart(0) - dart(2), dart(1) - dart(3),
        '        dart(2) * 2, dart(3) * 2), New MCvScalar(0, 255, 0))
        'Next
        'Imwrite("detected.jpg", image)
        'image.Dispose()
        'Console.ReadLine()

        P_Test("ell")

        ''W9.S_L1T4_0()
        'W9.S_L1T4()
        ''W9.S_ImageSeg()
        ''W9.CannyTest()

        'Console.ReadLine()
    End Sub

    Public Sub P_Test(mode As String)

        If mode = "pt4" Then
            'W9.S_L1T4_0()
            W9.S_L1T4()
        ElseIf mode = "ell" Then
            P_EllipseHough()
        ElseIf mode = "gen" Then
            P_GHT_Sample()
        ElseIf mode = "fil" Then
            P_LineCircleFilter()
        ElseIf mode = "quad" Then
            P_Quad()
        ElseIf mode = "win" Then
            P_SlidingWindow()
        ElseIf mode = "water" Then
            P_Watershed()
        ElseIf mode = "chan" Then
            P_ChanSplit()
        ElseIf mode = "canny" Then
            P_CannySobel()
        Else
            '...

        End If
    End Sub


End Module
