Public Class LangHive

    Private _en As Dictionary(Of String, String)
    Private _fr As Dictionary(Of String, String)

    Public Sub New()
        _en = New Dictionary(Of String, String)
        _fr = New Dictionary(Of String, String)
    End Sub

    Private Function GetDic(lang As String) As Dictionary(Of String, String)
        If lang = "en" Then
            Return _en
        ElseIf lang = "fr" Then
            Return _fr
        Else
            Throw New Exception("language not en or fr found")
        End If
    End Function

    Private Function EKey(key1 As String, Optional key2 As String = "") As String
        'E for ENHANCE

        Return key1 & key2 ' very complicated

    End Function

    Public Sub AddItem(key As String, enText As String, frText As String, Optional key2 As String = "")
        Dim kkey = EKey(key, key2)
        _en.Add(kkey, enText)
        _fr.Add(kkey, frText)
    End Sub

    Public Function Txt(lang As String, key As String, Optional key2 As String = "") As String
        Dim dic As Dictionary(Of String, String)
        Dim kkey = EKey(key, key2)
        dic = GetDic(lang)

        Return dic.Item(kkey)

    End Function

End Class
