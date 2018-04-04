﻿Imports System.IO

Public Class ConfigForm
    Private Sub cmdEnhance_Click(sender As Object, e As EventArgs) Handles cmdEnhance.Click

        'load the two template files
        Dim oSrcEn As New StreamReader("c:\code\configotron\configotron\template.en.json")
        Dim oSrcFr As New StreamReader("c:\code\configotron\configotron\template.fr.json")

        Dim sSrcEn As String = oSrcEn.ReadToEnd()
        oSrcEn.Close()

        Dim sSrcFr As String = oSrcFr.ReadToEnd()
        oSrcFr.Close()

        Dim sSrc As String

        'param arrays
        Dim aRcp = {"rcp26", "rcp45", "rcp85"}
        Dim aVar = {"snow", "ice_thickness", "ice_fraction", "wind"}
        Dim aSub = {"ANN", "MAM", "JJA", "SON", "DJF"}
        Dim aLang = {"en", "fr"}

        For Each lang As String In aLang
            If lang = "en" Then
                sSrc = sSrcEn
            Else
                sSrc = sSrcFr
            End If

            For Each var As String In aVar
                For Each subp As String In aSub
                    For Each rcp As String In aRcp
                        MakeConfig(sSrc, var, subp, rcp, lang)
                    Next
                Next
            Next

        Next


        MsgBox("DONE THANKS")

    End Sub

    Private Function MakeFileName(variable As String, subPeroid As String, rcp As String, lang As String) As String
        Return "config-cmip5-" & variable & "-" & subPeroid & "-" & rcp & "-" & lang & "-CA.json"
    End Function

    Private Function MakeLayerURL(variable As String, subPeroid As String, rcp As String, year As String) As String
        ' e.g. http://cipgis.canadaeast.cloudapp.azure.com/arcgis/rest/services/CMIP5/SeaIceThickness/SeaIceThickness_2061_20yr_SON_rcp45/MapServer

        Const roooot As String = "http://cipgis.canadaeast.cloudapp.azure.com/arcgis/rest/services/CMIP5/"

        Dim varfancy As String = ""
        Select Case variable
            Case "snow"
                varfancy = "SnowDepth"
            Case "ice_thickness"
                varfancy = "SeaIceThickness"
            Case "ice_fraction"
                varfancy = "SeaIceFraction"
            Case "wind"
                varfancy = "WindSpeed"
        End Select

        Return roooot & varfancy & "/" & varfancy & "_" & year & "_20yr_" & subPeroid & "_" & rcp & "/MapServer"
    End Function

    Private Function MakeLayerSnippet(variable As String, subPeroid As String, rcp As String, year As String, trailingComma As Boolean) As String
        '{
        '  "id":"canadaElevation",
        '  "name": "Canada Elevation",
        '  "layerType":"esriTile",
        '  "url":"http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBME_CBCE_HS_RO_3978/MapServer",
        '  "state": {
        '    "opacity": 0.5,
        '    "visibility": false
        '  }
        '}
        Const pad As String = "      "
        Const pad2 As String = "        "
        Const pad3 As String = "          "

        Dim url = MakeLayerURL(variable, subPeroid, rcp, year)
        Dim splitter As String() = url.Split("/")

        Dim json As String = pad & "{" & vbCrLf &
            pad2 & """id"": """ & splitter(splitter.Length - 2) & """," & vbCrLf &
            pad2 & """layerType"": ""esriTile""," & vbCrLf &
            pad2 & """url"": """ & url & """," & vbCrLf &
            pad2 & """state"": {" & vbCrLf &
            pad3 & """opacity"": 0.8," & vbCrLf &
            pad3 & """visibility"": false" & vbCrLf &
            pad2 & "}" & vbCrLf &
            pad & "}" & IIf(trailingComma, ",", "") & vbCrLf

        Return json

    End Function

    Private Function MakeLayerSet(variable As String, subPeroid As String, rcp As String) As String
        Dim aYear = {"2021", "2041", "2061", "2081"}
        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeLayerSnippet(variable, subPeroid, rcp, year, year <> "2081")
        Next

        Return lset

    End Function

    Private Sub MakeConfig(template As String, variable As String, subPeroid As String, rcp As String, lang As String)

        Dim oFile As StreamWriter = New StreamWriter("c:\code\configotron\configotron\dump\" & MakeFileName(variable, subPeroid, rcp, lang), False)

        Dim sLayerSet = MakeLayerSet(variable, subPeroid, rcp)
        oFile.Write(template.Replace("LAYERS_SPOT", sLayerSet))
        oFile.Close()

    End Sub


End Class
