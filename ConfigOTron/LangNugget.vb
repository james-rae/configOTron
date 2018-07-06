Public Class LangNugget

    Private _en As String
    Private _fr As String

    Public Property en As String
        Get
            Return _en
        End Get
        Set(value As String)
            _en = value
        End Set
    End Property

    Public Property fr As String
        Get
            Return _fr
        End Get
        Set(value As String)
            _fr = value
        End Set
    End Property

    Public Sub setLang(ByVal lang As String, ByVal guts As String)
        'just to avoid doing property by string var.   complex function here, kids

        If lang = "en" Then
            Me.en = guts
        ElseIf lang = "fr" Then
            Me.fr = guts
        Else
            Throw New Exception("language not en or fr found")
        End If
    End Sub

End Class
