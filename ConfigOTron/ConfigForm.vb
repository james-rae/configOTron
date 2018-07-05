Imports System.IO

Public Class ConfigForm

    ' arrays of domains. global scope for sharing fun
    Shared aRcp = {"rcp26", "rcp45", "rcp85"}
    Shared aCMIP5Var = {"snow", "sith", "sico", "wind"}
    Shared aSeason = {"ANN", "MAM", "JJA", "SON", "DJF"}
    Shared aYear = {"2021", "2041", "2061", "2081"}

    Const DUMP_FOLDER As String = "c:\git\configotron\configotron\dump\"
    Const PAD1 As String = "    "
    Const PAD2 As String = "      "
    Const PAD3 As String = "        "

    Private Sub cmdEnhanceMini_Click(sender As Object, e As EventArgs) Handles cmdEnhanceMini.Click

        'MAIN STARTING POINT OF APP.

        MakeCMIP5Configs()

        MsgBox("DONE THANKS")
    End Sub



    Private Function MakeMiniFileName(variable As String, subPeroid As String, rcp As String) As String

        Dim badAlyVar As String = ""
        Dim badAlyPeriod As String = ""

        Select Case subPeroid
            Case "DJF"
                badAlyPeriod = "winter"
            Case "JJA"
                badAlyPeriod = "summer"
            Case "MAM"
                badAlyPeriod = "spring"
            Case "SON"
                badAlyPeriod = "fall"
            Case "ANN"
                badAlyPeriod = "annual"
        End Select

        Select Case variable
            Case "snow"
                badAlyVar = "snd"
            Case "sith"
                badAlyVar = "sit"
            Case "sico"
                badAlyVar = "sic"
            Case "wind"
                badAlyVar = "sfcwind"
        End Select

        Return "cmip5-layer-configs-" & badAlyVar & "-" & badAlyPeriod & "-" & rcp & ".json"
    End Function

    Private Function MakeLayerURL(variable As String, subPeroid As String, rcp As String, year As String) As String
        ' e.g. http://cipgis.canadaeast.cloudapp.azure.com/arcgis/rest/services/CMIP5_SeaIceThickness/SeaIceThickness_2061_20yr_SON_rcp45/MapServer

        Dim roooot As String = "http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/CMIP5/"

        'Dim varfancy As String = ""
        'Select Case variable
        '    Case "snow"
        '        varfancy = "SNOW"
        '    Case "sith"
        '        varfancy = "SeaIceThickness"
        '    Case "sico"
        '        varfancy = "SeaIceConcentration"
        '    Case "wind"
        '        varfancy = "WindSpeed"
        'End Select

        Return roooot & "CMIP5_" & UCase(variable) & "/MapServer"
    End Function

    Private Function BoolToJson(bool As Boolean) As String
        'complicated
        If bool Then
            Return "true"
        Else
            Return "false"
        End If
    End Function

    ''' <summary>
    ''' Generates a config structure that defines a WMS layer
    ''' </summary>
    ''' <param name="url"></param>
    ''' <param name="id"></param>
    ''' <param name="opacity"></param>
    ''' <param name="visible"></param>
    ''' <param name="layer"></param>
    ''' <returns></returns>
    Private Function MakeWMSLayerConfig(url As String, id As String, opacity As Double, visible As Boolean, layer As String) As String
        '{
        '  "id":"canadaElevation",
        '  "layerType":"ogcWMS",
        '  "url":"http://geo.weather.gc.ca/geomet/?lang=E&service=WMS&request=GetCapabilities",
        '  "state": {
        '    "opacity": 0.5,
        '    "visibility": false
        '  },
        '  "layerEntries" [
        '    {
        '      "id": "GDPS.ETA_UU"
        '    }
        '  ]
        '}

        'TODO if we need the 'name' element, will need some translations and the lang param coming in
        'TODO do we need to have a STYLE parameter added?
        'TODO most likely remove data parameter, unless we add in json table, then might need it

        Dim json As String = pad1 & "{" & vbCrLf &
            pad2 & """id"": """ & id & """," & vbCrLf &
            pad2 & """layerType"":  ""ogcWMS""," & vbCrLf &
            pad2 & """url"": """ & url & """," & vbCrLf &
            pad2 & """state"": {" & vbCrLf &
            pad3 & """opacity"": " & opacity & "," & vbCrLf &
            pad3 & """visibility"": " & BoolToJson(visible) & vbCrLf &
            pad2 & "}," & vbCrLf &
            pad2 & """layerEntries"": [{""id"": """ & layer & """ }]," & vbCrLf &
            pad2 & """controls"": [""data""]" & vbCrLf &
            pad1 & "}" & vbCrLf

        Return json

    End Function


    ''' <summary>
    ''' Generates a config structure that defines a Tile layer
    ''' </summary>
    ''' <param name="url"></param>
    ''' <param name="id"></param>
    ''' <param name="opacity"></param>
    ''' <param name="visible"></param>
    ''' <returns></returns>
    Private Function MakeTileLayerConfig(url As String, id As String, opacity As Double, visible As Boolean) As String
        '{
        '  "id":"canadaElevation",
        '  "layerType":"esriTile",
        '  "url":"http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBME_CBCE_HS_RO_3978/MapServer",
        '  "state": {
        '    "opacity": 0.5,
        '    "visibility": false
        '  }
        '}

        'TODO if we need the 'name' element, will need some translations and the lang param coming in
        'TODO verify that we can delete the "control" part for tiles.
        Dim json As String = pad1 & "{" & vbCrLf &
            pad2 & """id"": """ & id & """," & vbCrLf &
            pad2 & """layerType"": ""esriTile""," & vbCrLf &
            pad2 & """url"": """ & url & """," & vbCrLf &
            pad2 & """state"": {" & vbCrLf &
            pad3 & """opacity"": " & opacity & "," & vbCrLf &
            pad3 & """visibility"": " & BoolToJson(visible) & vbCrLf &
            pad2 & "}," & vbCrLf &
            pad2 & """controls"": [""data""]" & vbCrLf &
            pad1 & "}"

        Return json

    End Function

    ''' <summary>
    ''' Makes the overall structure of a config file
    ''' </summary>
    ''' <param name="legendPart"></param>
    ''' <param name="supportLayerPart"></param>
    ''' <param name="dataLayerPart"></param>
    ''' <returns></returns>
    Private Function MakeConfigStructure(legendPart As String, supportLayerPart As String, dataLayerPart As String) As String
        '3 named arrays of stuff
        Dim json As String = "{" & vbCrLf &
            "  ""legend"": [" & vbCrLf &
            legendPart & vbCrLf &
            "  ]," & vbCrLf &
            "  ""supportLayers"": [" & vbCrLf &
            supportLayerPart & vbCrLf &
            "  ]," & vbCrLf &
            "  ""dataLayers"": [" & vbCrLf &
            dataLayerPart & vbCrLf &
            "  ]" & vbCrLf &
            "}" & vbCrLf

        Return json
    End Function

    Private Function MakeLayerSet(variable As String, subPeroid As String, rcp As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeLayerSnippet(variable, subPeroid, rcp, year, year <> "2081")
        Next

        Return lset

    End Function

    ''' <summary>
    ''' Writes content to a text file
    ''' </summary>
    ''' <param name="filename"></param>
    ''' <param name="content"></param>
    Private Sub WriteConfig(filename As String, content As String)

        Dim oFile As StreamWriter = New StreamWriter(DUMP_FOLDER & filename, False)
        oFile.Write(content)
        oFile.Close()

    End Sub



#Region " CMIP5 "

    Private Sub MakeCMIP5Configs()
        For Each var As String In aCMIP5Var
            For Each season As String In aSeason
                For Each rcp As String In aRcp

                    Dim dataLayers = MakeCMIP5YearSet(var, season, rcp)
                    Dim legund = MakeCMIP5Legend(var, season, rcp)

                    Dim fileguts = MakeConfigStructure(legund, "", dataLayers)
                    WriteConfig("test_" & var & season & rcp & ".json", fileguts)

                Next
            Next
        Next
    End Sub

    ''' <summary>
    ''' Makes the Data Layer array for CMIP5 "set of four time periods"
    ''' </summary>
    ''' <param name="variable"></param>
    ''' <param name="season"></param>
    ''' <param name="rcp"></param>
    ''' <returns></returns>
    Private Function MakeCMIP5YearSet(variable As String, season As String, rcp As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeCMIP5DataLayer(variable, season, rcp, year) & IIf(year <> "2081", ",", "") & vbCrLf
        Next

        Return lset

    End Function

    Private Function MakeCMIP5DataLayer(variable As String, season As String, rcp As String, year As String) As String

        'calculate url (might be a constant)
        'calculate wms layer id
        'derive unique layer id (ramp id)

        Return MakeWMSLayerConfig("url", "id", 1, True, "wmslayer")

    End Function

    Private Function MakeCMIP5Legend(variable As String, season As String, rcp As String) As String

        Return "{ ""legend"": true }"

    End Function

#End Region

#Region " Legacy Code "

    Function MagicIndex(rcp As String, subPeroid As String, year As String) As String
        Dim yearI, periodI, rcpI As Integer

        Select Case rcp
            Case "rcp26"
                rcpI = 0
            Case "rcp45"
                rcpI = 26
            Case "rcp85"
                rcpI = 52
        End Select

        Select Case subPeroid
            Case "DJF"
                periodI = 21
            Case "JJA"
                periodI = 11
            Case "MAM"
                periodI = 6
            Case "SON"
                periodI = 16
            Case "ANN"
                periodI = 1
        End Select

        Select Case year
            Case "2021"
                yearI = 1
            Case "2041"
                yearI = 2
            Case "2061"
                yearI = 3
            Case "2081"
                yearI = 4
        End Select

        Return periodI + yearI + rcpI

    End Function

    Private Sub cmdEnhance_Click(sender As Object, e As EventArgs)

        'load the two template files
        Dim oSrcEn As New StreamReader("c:\git\configotron\configotron\template.en.json")
        Dim oSrcFr As New StreamReader("c:\git\configotron\configotron\template.fr.json")

        Dim sSrcEn As String = oSrcEn.ReadToEnd()
        oSrcEn.Close()

        Dim sSrcFr As String = oSrcFr.ReadToEnd()
        oSrcFr.Close()

        Dim sSrc As String

        'param arrays
        Dim aRcp = {"rcp26", "rcp45", "rcp85"}
        Dim aVar = {"snow", "sith", "sico", "wind"}
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

    Private Sub MakeConfig(template As String, variable As String, subPeroid As String, rcp As String, lang As String)
        ' this makes a full on config file
        Dim oFile As StreamWriter = New StreamWriter("c:\git\configotron\configotron\dump\" & MakeFileName(variable, subPeroid, rcp, lang), False)

        Dim sLayerSet = MakeLayerSet(variable, subPeroid, rcp)
        oFile.Write(template.Replace("LAYERS_SPOT", sLayerSet))
        oFile.Close()

    End Sub

    Private Sub MakeMiniConfig(template As String, variable As String, subPeroid As String, rcp As String)
        ' this just makes a small config with layer snippets for one set. it doesn't have the full config contents
        Dim oFile As StreamWriter = New StreamWriter("c:\git\configotron\configotron\dump\" & MakeMiniFileName(variable, subPeroid, rcp), False)

        Dim sLayerSet = MakeLayerSet(variable, subPeroid, rcp)
        oFile.Write(template.Replace("LAYERS_SPOT", sLayerSet))
        oFile.Close()

    End Sub

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

        'TODO if we need the 'name' element, will need some translations and the lang param coming in
        Const pad As String = "      "
        Const pad2 As String = "        "
        Const pad3 As String = "          "

        Dim url = MakeLayerURL(variable, subPeroid, rcp, year)
        Dim magic = MagicIndex(rcp, subPeroid, year)

        Dim json As String = pad & "{" & vbCrLf &
            pad2 & """id"": """ & variable & "_" & magic & """," & vbCrLf &
            pad2 & """layerType"": ""esriDynamic""," & vbCrLf &
            pad2 & """url"": """ & url & """," & vbCrLf &
            pad2 & """state"": {" & vbCrLf &
            pad3 & """opacity"": 0.85," & vbCrLf &
            pad3 & """visibility"": false" & vbCrLf &
            pad2 & "}," & vbCrLf &
            pad2 & """layerEntries"": [{""index"": " & magic & " }]," & vbCrLf &
            pad2 & """controls"": [""data""]" & vbCrLf &
            pad & "}" & IIf(trailingComma, ",", "") & vbCrLf

        Return json

    End Function

#End Region

End Class
