﻿Public Class ConfigNugget

    Private _guts As String
    Private _rootPad As String
    Const indentSize As Integer = 2 ' number of spaces in an indent

    Public Sub New(ByVal iRootPad As Integer)
        _rootPad = iRootPad
        _guts = ""
    End Sub

    Public ReadOnly Property Nugget As String
        Get
            Return _guts
        End Get
    End Property

    Public Sub AddLine(ByVal sLine As String, Optional ByVal iPadLevel As Integer = 0, Optional bNoCrLF As Boolean = False)
        Dim sL As String

        sL = MakePad(iPadLevel) & sLine & IIf(bNoCrLF, "", vbCrLf)

        _guts &= sL

    End Sub

    Private Function MakePad(iPadSize As Integer) As String
        Return Space((iPadSize + _rootPad) * indentSize)
    End Function

End Class
