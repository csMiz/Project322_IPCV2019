' Version: 1.0.1
' Author: Frank

Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.CvInvoke
Imports Emgu.CV.Util

''' <summary>
''' 解决.net环境下openCV的api与标准不同的问题
''' </summary>
Module OCV_Helper

    ''' <summary>
    ''' 对Mat图像单个像素点赋值BGR，相当于'At'
    ''' </summary>
    ''' <param name="mat">Mat图像</param>
    ''' <param name="row">行</param>
    ''' <param name="col">列</param>
    ''' <param name="value">BGR - 3 Bytes</param>
    Public Sub Mat_SetPixel_3(ByRef mat As Mat, row As Integer, col As Integer, value As Byte())
        Runtime.InteropServices.Marshal.Copy _
            (value, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 3)
    End Sub

    ''' <summary>
    ''' 对Mat图像单个像素点赋值灰度，相当于'At'
    ''' </summary>
    ''' <param name="mat">Mat图像</param>
    ''' <param name="row">行</param>
    ''' <param name="col">列</param>
    ''' <param name="value">灰度 - 1 Byte</param>
    Public Sub Mat_SetPixel_1(ByRef mat As Mat, row As Integer, col As Integer, value As Byte)
        Runtime.InteropServices.Marshal.Copy _
            ({value}, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1)
    End Sub

    ''' <summary>
    ''' 对Mat图像单个像素点赋值Single，相当于'At'
    ''' </summary>
    ''' <param name="mat">Mat图像</param>
    ''' <param name="row">行</param>
    ''' <param name="col">列</param>
    ''' <param name="value">Single - 4 Bytes</param>
    Public Sub Mat_SetPixel_4(ByRef mat As Mat, row As Integer, col As Integer, value As Byte())
        Runtime.InteropServices.Marshal.Copy _
            (value, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 4)
    End Sub

    ''' <summary>
    ''' 根据fft结果的复数计算magnitude
    ''' </summary>
    ''' <param name="fftData">双通道32F</param>
    ''' <returns></returns>
    Public Function Magnitude(fftData As Mat) As Mat
        '傅里叶变换的实部
        Dim Real As Mat = New Mat(fftData.Size, DepthType.Cv32F, 1)
        '傅里叶变换的虚部
        Dim Imaginary As Mat = New Mat(fftData.Size, DepthType.Cv32F, 1)
        Dim channels As VectorOfMat = New VectorOfMat()
        Split(fftData, channels)   '将多通道mat分离成几个单通道mat
        Real = channels.GetOutputArray().GetMat(0)
        Imaginary = channels.GetOutputArray().GetMat(1)
        Pow(Real, 2.0, Real)
        Pow(Imaginary, 2.0, Imaginary)
        Add(Real, Imaginary, Real)
        Pow(Real, 0.5, Real)
        Dim onesMat As Mat = New Mat(Real.Rows, Real.Cols, DepthType.Cv32F, 1)
        For j = 0 To Real.Cols - 1
            For i = 0 To Real.Rows - 1
                Mat_SetPixel_4(onesMat, j, i, BitConverter.GetBytes(1.0F))
            Next
        Next
        Add(Real, onesMat, Real)
        Log(Real, Real)    '求自然对数
        Return Real
    End Function

    ''' <summary>
    ''' 对图像进行dft
    ''' </summary>
    ''' <param name="image">32_Float类型灰度图</param>
    ''' <returns></returns>
    Public Function GetImageFourierTransformation(image As Mat) As Mat
        'merge input
        Dim input As New VectorOfMat
        input.Push(image)
        Dim emptyMat As New Mat(512, 512, DepthType.Cv32F, 1)
        input.Push(emptyMat)
        Dim complex As New Mat(image.Size, DepthType.Cv32F, 2)
        Merge(input, complex)
        'calculate dft
        Dft(complex, complex, DxtType.Forward, 512)
        'convert to magnitude
        Dim mag As Mat = Magnitude(complex)
        Normalize(mag, mag, 0.0F, 1.0F, NormType.MinMax)
        'change quadrant
        Dim res As New Mat(mag.Size, DepthType.Cv32F, 1)
        Dim halfH As Integer = mag.Size.Height / 2
        Dim halfW As Integer = mag.Size.Width / 2
        For j = 0 To mag.Size.Height - 1
            For i = 0 To mag.Size.Width - 1
                If i < halfW AndAlso j < halfH Then
                    Mat_SetPixel_4(res, j + halfH, i + halfW, mag.GetRawData(j, i))
                ElseIf i < halfW AndAlso j >= halfH Then
                    Mat_SetPixel_4(res, j - halfH, i + halfW, mag.GetRawData(j, i))
                ElseIf i >= halfW AndAlso j < halfH Then
                    Mat_SetPixel_4(res, j + halfH, i - halfW, mag.GetRawData(j, i))
                ElseIf i >= halfW AndAlso j >= halfH Then
                    Mat_SetPixel_4(res, j - halfH, i - halfW, mag.GetRawData(j, i))
                End If
            Next
        Next
        'dispose
        emptyMat.Dispose()
        input.Dispose()
        complex.Dispose()
        mag.Dispose()
        'return
        Return res
    End Function

End Module
