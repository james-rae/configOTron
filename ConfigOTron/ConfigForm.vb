Imports System.IO


'OUTSTANDING THINGS
' - adjust urls on WMS layers so they don't hit GetCapabilities
' - add getFeatureInfo settings to WMS layers
' - adjust urls on station layers (daily, monthly, normals(?)) for additional filtering for "one point per station"
' - update language strings
' - proper image svg for hydro legend

Public Class ConfigForm

    'I'm lazy so make sure you add the appropriate subfolders to your dump folder.  Get a student to do it.
    Const DUMP_FOLDER As String = "c:\git\configotron\configotron\dump\"


    ' arrays of domains. global scope for sharing fun
    Dim aLang = {"en", "fr"}
    Dim aRcp = {"rcp26", "rcp45", "rcp85"}
    Dim aAHCCDVar = {"tmean", "tmin", "tmax", "prec", "supr", "slpr", "wind"}
    Dim aCanGRIDVar = {"tmean", "prec"} ' "tmin", "tmax",
    Dim aCanSIPSVar = {"slpr", "itpr", "stpr", "wtpr", "gh5m", "ta8m", "wd2m", "wd8m"}
    Dim aCAPAVar = {"qp25", "qp10"}
    Dim aCMIP5Var = {"snow", "sith", "sico", "wind", "tmean", "prec"}
    Dim aDailyVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aDCSVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aMonthlyVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aNormalsVar = {"tmean", "tmin", "tmax", "prec"} ' , "stpr", "slpr", "wind", "mgst", "dgst"}

    Dim aSeason = {"ANN", "MAM", "JJA", "SON", "DJF"}
    Dim aSeasonMonth = {"ANN", "MAM", "JJA", "SON", "DJF", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"}
    Dim aSeasonMonthOnly = {"ANN", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"}
    Dim aSeasonMonthly = {"ANN", "MAM", "JJA", "SON", "DJF", "MTH"}
    Dim aYear = {"2021", "2041", "2061", "2081"}
    Dim aHour = {"24", "6"} 'want this order on time slider

    'language hives. global scope for sharing fun
    Dim oCommonLang As LangHive
    Dim oAHCCDLang As LangHive
    Dim oCanGRIDLang As LangHive
    Dim oCanSIPSLang As LangHive
    Dim oCAPALang As LangHive
    Dim oCMIP5Lang As LangHive
    Dim oDailyLang As LangHive
    Dim oDCSLang As LangHive
    Dim oHydroLang As LangHive
    Dim oMonthlyLang As LangHive
    Dim oNormalsLang As LangHive

    'shared layer ids
    Const LABELS_LAYER_ID As String = "labels"
    Const PROVINCES_LAYER_ID As String = "provinces"
    Const CITIES_LAYER_ID As String = "cities"

    'common lang keys
    Const TOP_TITLE As String = "TopTitle"
    Const TOP_DESC As String = "TopDesc"
    Const LAYER_NAME As String = "LayerName"
    Const VAR_DESC As String = "VarDesc"
    Const COVER_ICON As String = "CovIcon"
    Const SETTINGS_TITLE As String = "SettingsTitle"
    Const LEGEND_TEXT As String = "LegendText"

    Private Sub cmdEnhanceMini_Click(sender As Object, e As EventArgs) Handles cmdEnhanceMini.Click

        'MAIN STARTING POINT OF APP.
        MakeCommonLang()

        MakeCMIP5Configs()
        MakeDCSConfigs()
        MakeAHCCDConfigs()
        MakeCAPAConfigs()
        MakeHydroConfigs()
        MakeCanGRIDConfigs()
        MakeDailyConfigs()
        MakeMonthlyConfigs()
        MakeNormalsConfigs()
        MakeCanSIPSConfigs()

        MsgBox("DONE THANKS")
    End Sub

    Private Sub MakeCommonLang()
        oCommonLang = New LangHive
        With oCommonLang
            'support layer names
            .AddItem(LAYER_NAME, "Labels", "[fr] Labels", LABELS_LAYER_ID)
            .AddItem(LAYER_NAME, "Provinces", "[fr] Provinces", PROVINCES_LAYER_ID)
            .AddItem(LAYER_NAME, "Cities", "[fr] Cities", CITIES_LAYER_ID)
            .AddItem(SETTINGS_TITLE, "Settings", "[fr] Settings")
        End With
    End Sub

#Region " General Structure Builders "

    ''' <summary>
    ''' Turns native boolean into json text boolean
    ''' </summary>
    ''' <param name="bool"></param>
    ''' <returns></returns>
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
    ''' <param name="rampId"></param>
    ''' <param name="opacity"></param>
    ''' <param name="visible"></param>
    ''' <param name="wmsLayerId"></param>
    ''' <returns></returns>
    Private Function MakeWMSLayerConfig(url As String, rampId As String, opacity As Double, visible As Boolean, wmsLayerId As String, layerName As String,
                                     Optional mimeType As String = "", Optional template As String = "", Optional parser As String = "", Optional visibleToggle As Boolean = False) As String
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

        'TODO do we need to have a STYLE parameter added?
        'TODO most likely remove data parameter, unless we add in json table, then might need it

        Dim nugget As New ConfigNugget(2)

        'TODO add something like
        ' "featureInfoMimeType": "text/plain",

        nugget.AddLine("{")
        nugget.AddLine("""id"": """ & rampId & """,", 1)
        nugget.AddLine("""layerType"": ""ogcWms"",", 1)
        nugget.AddLine("""suppressGetCapabilities"": true,", 1)
        nugget.AddLine("""url"": """ & url & """,", 1)
        nugget.AddLine("""name"": """ & layerName & """,", 1)
        nugget.AddLine("""state"": {", 1)
        nugget.AddLine("""opacity"": " & opacity & ",", 2)
        nugget.AddLine("""visibility"": " & BoolToJson(visible), 2)
        nugget.AddLine("},", 1)
        nugget.AddLine("""layerEntries"": [{""id"": """ & wmsLayerId & """ }],", 1)

        If mimeType <> "" Then
            nugget.AddLine("""featureInfoMimeType"": """ & mimeType & """,", 1)
        End If

        InjectTemplate(nugget, 1, template, parser)
        nugget.AddLine("""controls"": [""data""" & IIf(visibleToggle, ", ""visibility""", "") & "]", 1)
        nugget.AddLine("}", 0, True)

        Return nugget.Nugget

    End Function


    ''' <summary>
    ''' Generates a config structure that defines a Tile layer
    ''' </summary>
    ''' <param name="url"></param>
    ''' <param name="id"></param>
    ''' <param name="opacity"></param>
    ''' <param name="visible"></param>
    ''' <returns></returns>
    Private Function MakeTileLayerConfig(url As String, id As String, opacity As Double, visible As Boolean, layerName As String,
                                          Optional template As String = "", Optional parser As String = "", Optional toastTime As String = "4000") As String
        '{
        '  "id":"canadaElevation",
        '  "layerType":"esriTile",
        '  "url":"http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBME_CBCE_HS_RO_3978/MapServer",
        '  "state": {
        '    "opacity": 0.5,
        '    "visibility": false
        '  }
        '}

        'TODO verify that we can delete the "control" part for tiles.
        'TODO we might also want "controls": ["visibility", "opacity", "reload", "settings"],

        Dim nugget As New ConfigNugget(2)

        nugget.AddLine("{")
        nugget.AddLine("""id"": """ & id & """,", 1)
        nugget.AddLine("""layerType"": ""esriTile"",", 1)
        nugget.AddLine("""url"": """ & url & """,", 1)
        nugget.AddLine("""name"": """ & layerName & """,", 1)
        nugget.AddLine("""expectedResponseTime"": " & toastTime & ",", 1)
        InjectTemplate(nugget, 1, template, parser)
        nugget.AddLine("""state"": {", 1)
        nugget.AddLine("""opacity"": " & opacity & ",", 2)
        nugget.AddLine("""visibility"": " & BoolToJson(visible), 2)
        nugget.AddLine("}", 1)
        'nugget.AddLine("""controls"": [""data""]", 1)
        nugget.AddLine("}", 0, True)

        Return nugget.Nugget

    End Function

    ''' <summary>
    ''' will insert a template section to a layer config
    ''' the sourceNugget contents are modified by this function.
    ''' We assume its not the last item in the layer object, due to lazyness
    ''' </summary>
    ''' <param name="sourceNugget"></param>
    ''' <param name="startingLevel"></param>
    ''' <param name="template"></param>
    ''' <param name="parser"></param>
    Private Sub InjectTemplate(sourceNugget As ConfigNugget, startingLevel As Integer, Optional template As String = "", Optional parser As String = "")
        Dim bT As Boolean = (template <> "")
        Dim bP As Boolean = (parser <> "")
        If (Not bT) And (Not bP) Then
            Exit Sub
        End If

        sourceNugget.AddLine("""details"": {", startingLevel)
        If bT Then
            sourceNugget.AddLine("""template"": """ & template & """" & IIf(bP, ",", ""), startingLevel + 1)
        End If

        If bP Then
            sourceNugget.AddLine("""parser"": """ & parser & """", startingLevel + 1)
        End If

        sourceNugget.AddLine("},", startingLevel)

    End Sub

    Private Function MakeWFSLayerConfig(url As String, id As String, opacity As Double, visible As Boolean, nameField As String, layerName As String, colour As String,
                                        Optional template As String = "", Optional parser As String = "") As String
        '{
        '  "id":"canadaElevation",
        '  "layerType":"ogcWfs",
        '  "url":"http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=temp_mean",
        '  "nameField": "station_id",
        '  "state": {
        '    "opacity": 0.5,
        '    "visibility": false
        '  },
        '  "controls": ["data"]
        '}

        Dim nugget As New ConfigNugget(2)

        nugget.AddLine("{")
        nugget.AddLine("""id"": """ & id & """,", 1)
        nugget.AddLine("""layerType"": ""ogcWfs"",", 1)
        nugget.AddLine("""url"": """ & url & """,", 1)
        nugget.AddLine("""name"": """ & layerName & """,", 1)
        nugget.AddLine("""nameField"": """ & nameField & """,", 1)
        nugget.AddLine("""state"": {", 1)
        nugget.AddLine("""opacity"": " & opacity & ",", 2)
        nugget.AddLine("""visibility"": " & BoolToJson(visible), 2)
        nugget.AddLine("},", 1)
        InjectTemplate(nugget, 1, template, parser)
        nugget.AddLine("""colour"": """ & colour & """,", 1)  ' should be #112233 format
        nugget.AddLine("""controls"": [""data"", ""visibility"", ""opacity""]", 1)
        nugget.AddLine("}", 0, True)

        Return nugget.Nugget

    End Function


    ''' <summary>
    ''' Makes the structure of a config file for a language
    ''' </summary>
    ''' <param name="legendPart"></param>
    ''' <param name="supportLayerPart"></param>
    ''' <param name="dataLayerPart"></param>
    ''' <returns></returns>
    Private Function MakeConfigStructure(legendPart As String, supportLayerPart As String, dataLayerPart As String) As String
        '3 named arrays of stuff

        Dim nugget As New ConfigNugget(0)

        nugget.AddLine("{")
        nugget.AddLine("""legend"": [", 1)
        nugget.AddRaw(legendPart)
        nugget.AddLine("],", 1)
        nugget.AddLine("""supportLayers"": [", 1)
        nugget.AddRaw(supportLayerPart)
        nugget.AddLine("],", 1)
        nugget.AddLine("""dataLayers"": [", 1)
        nugget.AddRaw(dataLayerPart)
        nugget.AddLine("]", 1)
        nugget.AddLine("}", 0, True)

        Return nugget.Nugget


    End Function

    ''' <summary>
    ''' Makes a config file language structure, puts appropriate content under the language properties
    ''' </summary>
    ''' <param name="nugget"></param>
    ''' <returns></returns>
    Private Function MakeLangStructure(nugget As LangNugget) As String
        'should iterate through nugget...being lazy and hardcoding
        Dim json As String = "{" & vbCrLf &
            """en"": " &
            nugget.en & "," & vbCrLf &
            """fr"": " &
            nugget.fr & vbCrLf &
            "}"
        Return json
    End Function

    ''' <summary>
    ''' Makes legend block for simple info content
    ''' </summary>
    ''' <param name="infoType"></param>
    ''' <param name="content"></param>
    ''' <param name="indentLevel"></param>
    ''' <param name="trailingComma"></param>
    ''' <returns></returns>
    Private Function MakeSimpleLegendBlockConfig(infoType As String, content As String, indentLevel As Integer, Optional trailingComma As Boolean = True) As String

        Dim nugget As New ConfigNugget(indentLevel)

        nugget.AddLine("{")
        nugget.AddLine("""infoType"": """ & infoType & """,", 1)
        nugget.AddLine("""content"": """ & content & """", 1)
        nugget.AddLine("}" & IIf(trailingComma, ",", ""))

        Return nugget.Nugget

    End Function

    ''' <summary>
    ''' Makes a legend block for an overlay layer (e.g. tile on the map)
    ''' </summary>
    ''' <param name="layerName"></param>
    ''' <param name="layerId"></param>
    ''' <param name="icon"></param>
    ''' <param name="indentLevel"></param>
    ''' <param name="trailingComma"></param>
    ''' <returns></returns>
    Private Function MakeOverlayLegendBlockConfig(layerName As String, layerId As String, icon As String, indentLevel As Integer, Optional trailingComma As Boolean = True) As String

        Dim nugget As New ConfigNugget(indentLevel)

        nugget.AddLine("{")
        nugget.AddLine("""layerId"": """ & layerId & """,", 1)
        nugget.AddLine("""layerName"": """ & layerName & """,", 1)
        nugget.AddLine("""coverIcon"": """ & icon & """,", 1)
        nugget.AddLine("""symbologyStack"": []", 1)
        nugget.AddLine("}" & IIf(trailingComma, ",", ""))

        Return nugget.Nugget

    End Function

    ''' <summary>
    ''' Makes legend block for a data layer (tied to map layer)
    ''' </summary>
    ''' <param name="layerName"></param>
    ''' <param name="layerId"></param>
    ''' <param name="descrip"></param>
    ''' <param name="icon"></param>
    ''' <param name="legendImg"></param>
    ''' <param name="legendText"></param>
    ''' <param name="indentLevel"></param>
    ''' <param name="trailingComma"></param>
    ''' <returns></returns>
    Private Function MakeLayerLegendBlockConfig(layerName As String, layerId As String, descrip As String, icon As String,
          legendImg As String, legendText As String, indentLevel As Integer, Optional trailingComma As Boolean = True,
          Optional symbolStyle As String = "images") As String


        Dim nugget As New ConfigNugget(indentLevel)

        nugget.AddLine("{")
        nugget.AddLine("""layerId"": """ & layerId & """,", 1)
        nugget.AddLine("""layerName"": """ & layerName & """,", 1)
        nugget.AddLine("""description"": """ & descrip & """,", 1)
        nugget.AddLine("""coverIcon"": """ & icon & """,", 1)
        nugget.AddLine("""symbologyStack"": [", 1)
        nugget.AddLine("{", 2)
        nugget.AddLine("""image"": """ & legendImg & """,", 3)
        nugget.AddLine("""text"": """ & legendText & """", 3)
        nugget.AddLine("}", 2)
        nugget.AddLine("],", 1)
        nugget.AddLine("""symbologyRenderStyle"": """ & symbolStyle & """", 1)
        nugget.AddLine("}" & IIf(trailingComma, ",", ""))

        Return nugget.Nugget

    End Function

    Private Function MakeUnboundLayerLegendBlockConfig(layerName As String, descrip As String, icon As String,
          legendImg As String, legendText As String, indentLevel As Integer, Optional trailingComma As Boolean = True,
          Optional symbolStyle As String = "images") As String

        Dim nugget As New ConfigNugget(indentLevel)

        nugget.AddLine("{")
        nugget.AddLine("""infoType"": ""unboundLayer"",", 1)
        nugget.AddLine("""layerName"": """ & layerName & """,", 1)
        nugget.AddLine("""description"": """ & descrip & """,", 1)
        nugget.AddLine("""coverIcon"": """ & icon & """,", 1)
        nugget.AddLine("""symbologyStack"": [", 1)
        nugget.AddLine("{", 2)
        nugget.AddLine("""image"": """ & legendImg & """,", 3)
        nugget.AddLine("""text"": """ & legendText & """", 3)
        nugget.AddLine("}", 2)
        nugget.AddLine("],", 1)
        nugget.AddLine("""symbologyRenderStyle"": """ & symbolStyle & """", 1)
        nugget.AddLine("}" & IIf(trailingComma, ",", ""))

        Return nugget.Nugget

    End Function

    Private Function MakeLegendTitleConfig(titleText As String, descText As String) As String

        Dim json As String = MakeSimpleLegendBlockConfig("title", titleText, 2) &
             MakeSimpleLegendBlockConfig("text", descText, 2)

        Return json

    End Function


    ''' <summary>
    ''' Makes the legend structure for the settings block (mainly overlay layers)
    ''' </summary>
    ''' <param name="lang"></param>
    ''' <param name="city"></param>
    ''' <param name="prov"></param>
    ''' <param name="labels"></param>
    ''' <returns></returns>
    Private Function MakeLegendSettingsConfig(lang As String, city As Boolean, prov As Boolean, labels As Boolean) As String

        Const padLevel As Integer = 2

        Dim json As String = MakeSimpleLegendBlockConfig("title", oCommonLang.Txt(lang, SETTINGS_TITLE), padLevel)

        If city Then
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, CITIES_LAYER_ID), CITIES_LAYER_ID, "assets/images/cities.svg", padLevel, (prov Or labels))
        End If

        If labels Then
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, LABELS_LAYER_ID), LABELS_LAYER_ID, "assets/images/labels.svg", padLevel, prov)
        End If

        If prov Then
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, PROVINCES_LAYER_ID), PROVINCES_LAYER_ID, "assets/images/provinces.svg", padLevel, False)
        End If

        Return json

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

    Private Function FileSeason(season As String) As String

        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "annual"}, {"MAM", "spring"}, {"JJA", "summer"}, {"SON", "fall"}, {"DJF", "winter"}, {"JAN", "jan"},
            {"FEB", "feb"}, {"MAR", "mar"}, {"APR", "apr"}, {"MAY", "may"}, {"JUN", "jun"}, {"JUL", "jul"}, {"AUG", "aug"}, {"SEP", "sep"}, {"OCT", "oct"},
            {"NOV", "nov"}, {"DEC", "dec"}, {"MTH", "monthly"}}

        Return dSeason.Item(season)

    End Function


    Private Function FileVar(var As String) As String

        'TODO assuming common keys across similar vars (e.g. tmax is always same key).
        'restructure if not the case

        '"tmean", "tmin", "tmax", "prec", "supr", "slpr", "wind"
        Dim dVar As New Dictionary(Of String, String) From {{"wind", "sfcwind"}, {"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"supr", "stnpress"},
            {"slpr", "seapress"}, {"qp25", "hrdpa"}, {"qp10", "rdpa"}, {"snow", "snd"}, {"sith", "sit"}, {"sico", "sic"}, {"itpr", "xxxxitpr"}, {"stpr", "xxxxstpr"},
            {"wtpr", "xxxxwtpr"}, {"gh5m", "xxxxgh5m"}, {"ta8m", "xxxxta8m"}, {"wd2m", "xxxxwd2m"}, {"wd8m", "xxxxwd8m"}}

        Return dVar.Item(var)

    End Function

#End Region

#Region " Support Layers "

    Private Function MakeSupportSet(lang As String, city As Boolean, prov As Boolean, labels As Boolean) As String
        Dim sGuts As String = ""

        If city Then
            sGuts &= MakeCitiesConfig(lang) & "," & vbCrLf
        End If

        If labels Then
            sGuts &= MakeLabelsConfig(lang) & "," & vbCrLf
        End If

        If prov Then
            sGuts &= MakeProvinceConfig(lang) & "," & vbCrLf
        End If

        'trim last comma
        Return sGuts.Substring(0, sGuts.Length - 3)

    End Function

    Private Function MakeProvinceConfig(lang As String) As String

        Return MakeTileLayerConfig("http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/Overlays/Provinces/MapServer",
                                   PROVINCES_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, PROVINCES_LAYER_ID),,, "10000")
    End Function

    Private Function MakeCitiesConfig(lang As String) As String

        Return MakeTileLayerConfig("http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/Overlays/Cities/MapServer",
                                   CITIES_LAYER_ID, 1, False, oCommonLang.Txt(lang, LAYER_NAME, CITIES_LAYER_ID),,, "10000")
    End Function

    Private Function MakeLabelsConfig(lang As String) As String

        Return MakeTileLayerConfig("http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBMT_TXT_3978/MapServer",
                                   LABELS_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, LABELS_LAYER_ID),,, "10000")
    End Function

#End Region

#Region " CMIP5 "


    ' WMS. Time (by year).

    ''' <summary>
    ''' Create set of config files for CMIP5
    ''' </summary>
    Private Sub MakeCMIP5Configs()
        MakeCMIP5Lang()

        For Each var As String In aCMIP5Var
            For Each season As String In aSeason
                For Each rcp As String In aRcp
                    Dim nugget As New LangNugget
                    For Each lang As String In aLang

                        'TODO for now pick the first year and hack the key.  if we need all keys in the legend part, will need to abstract or duplicate the ramp key generator so its accessible to all parts
                        'derive unique layer id (ramp id)
                        'ideally we have unbound legend after covericon support is added
                        Dim rampID As String = "CMIP5_" & var & "_" & season & "_" & rcp & "_" & "2021" & "_" & lang

                        Dim dataLayers = MakeCMIP5YearSet(var, season, rcp, lang)
                        Dim legund = MakeCMIP5Legend(var, season, rcp, lang, rampID)
                        Dim support = MakeSupportSet(lang, True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)
                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("CMIP5\1\config-" & FileVar(var) & "-" & FileSeason(season) & "-" & rcp & ".json", fileguts)

                Next
            Next
        Next
    End Sub

    Private Sub MakeCMIP5Lang()
        Dim k As String 'lazy

        oCMIP5Lang = New LangHive

        With oCMIP5Lang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short CMIP5 dataset description goes here", "[fr] A short CMIP5 dataset description goes here")

            k = "snow"
            .AddItem(VAR_DESC, "A short snow depth description goes here", "[fr] A short snow depth description goes here", k)
            .AddItem(LAYER_NAME, "Snow depth", "[fr] Snow depth", k)

            k = "sith"
            .AddItem(VAR_DESC, "A short sea ice thickness description goes here", "[fr] A short sea ice thickness description goes here", k)
            .AddItem(LAYER_NAME, "Sea ice thickness", "[fr] Sea ice thickness", k)

            k = "sico"
            .AddItem(VAR_DESC, "A short sea ice concentration description goes here", "[fr] A short sea ice concentration description goes here", k)
            .AddItem(LAYER_NAME, "Sea ice concentration", "[fr] Sea ice concentration", k)

            k = "wind"
            .AddItem(VAR_DESC, "A short wind speed description goes here", "[fr] A short wind speed description goes here", k)
            .AddItem(LAYER_NAME, "Wind speed", "[fr] Wind speed", k)

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Total precipitation", "[fr] Total precipitation", k)

        End With

    End Sub

    ''' <summary>
    ''' Makes the Data Layer array for CMIP5 "set of four time periods"
    ''' </summary>
    ''' <param name="variable"></param>
    ''' <param name="season"></param>
    ''' <param name="rcp"></param>
    ''' <returns></returns>
    Private Function MakeCMIP5YearSet(variable As String, season As String, rcp As String, lang As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeCMIP5DataLayer(variable, season, rcp, year, lang) & IIf(year <> "2081", "," & vbCrLf, "")
        Next

        Return lset

    End Function

    Private Function MakeCMIP5DataLayer(variable As String, season As String, rcp As String, year As String, lang As String) As String

        'calculate url (might be a constant)
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=CMIP5.SND.RCP26.ANNUAL.2081-2100_PCTL50

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"snow", "SND"}, {"sith", "SIT"}, {"sico", "SIC"}, {"wind", "SFCWIND"}, {"tmean", "TM"}, {"prec", "PR"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "YEAR"}, {"MAM", "SPRING"}, {"JJA", "SUMMER"}, {"SON", "FALL"}, {"DJF", "WINTER"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim yearCode As String = year & "-" & CStr(CInt(year) + 19)
        Dim rcpCode As String = rcp.ToUpper()
        Dim template As String = "assets/templates/cmip5/variables-template.html"
        Dim parser As String = "assets/templates/cmip5/variables-script.js"

        Dim wmsCode As String = "CMIP5." & varCode & "." & rcpCode & "." & seasonCode & "." & yearCode & "_PCTL50"

        'derive unique layer id (ramp id)
        Dim rampID As String = "CMIP5_" & variable & "_" & season & "_" & rcp & "_" & year & "_" & lang

        Return MakeWMSLayerConfig(url, rampID, 1, False, wmsCode, oCMIP5Lang.Txt(lang, LAYER_NAME, variable), "text/plain", template, parser)

    End Function

    Private Function MakeCMIP5Legend(variable As String, season As String, rcp As String, lang As String, rampid As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        Dim dVari As New Dictionary(Of String, String) From {{"snow", "SND"}, {"sith", "SIT"}, {"sico", "SIC"}, {"wind", "SFCWIND"}, {"tmean", "TM"}, {"prec", "PR"}}
        Dim dIcon As New Dictionary(Of String, String) From {{"snow", "snd"}, {"sith", "sit"}, {"sico", "sic"}, {"wind", "sfcwind"}, {"tmean", "tmean"}, {"prec", "precip"}}

        sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CMIP5." & dVari.Item(variable) &
            ".RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oCMIP5Lang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampid, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)

            ' MakeUnboundLayerLegendBlockConfig(.Txt(lang, LAYER_NAME, variable), .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
        End With

        Return sLegend

    End Function


#End Region

#Region " DCS "

    ' WMS. Time (by year).

    ''' <summary>
    ''' Create set of config files for DCS
    ''' </summary>
    Private Sub MakeDCSConfigs()
        MakeDCSLang()

        For Each var As String In aDCSVar
            For Each season As String In aSeason
                For Each rcp As String In aRcp
                    Dim nugget As New LangNugget
                    For Each lang As String In aLang

                        'TODO for now pick the first year and hack the key.  if we need all keys in the legend part, will need to abstract or duplicate the ramp key generator so its accessible to all parts
                        'derive unique layer id (ramp id)
                        Dim rampID As String = "DCS_" & var & "_" & season & "_" & rcp & "_" & "2021" & "_" & lang

                        Dim dataLayers = MakeDCSYearSet(var, season, rcp, lang)  ' TODO we may need to add a 5th year period for "historical"
                        Dim legund = MakeDCSLegend(var, season, rcp, lang, rampID)
                        Dim support = MakeSupportSet(lang, True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)
                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("dcs\1\config-" & FileVar(var) & "-" & FileSeason(season) & "-" & rcp & ".json", fileguts)

                Next
            Next
        Next
    End Sub

    Private Sub MakeDCSLang()
        Dim k As String 'lazy

        oDCSLang = New LangHive

        With oDCSLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short DCS dataset description goes here", "[fr] A short DCS dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Precipitation", "[fr] Precipitation", k)

        End With

    End Sub

    ''' <summary>
    ''' Makes the Data Layer array for DCS "set of four time periods"
    ''' </summary>
    ''' <param name="variable"></param>
    ''' <param name="season"></param>
    ''' <param name="rcp"></param>
    ''' <returns></returns>
    Private Function MakeDCSYearSet(variable As String, season As String, rcp As String, lang As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeDCSDataLayer(variable, season, rcp, year, lang) & IIf(year <> "2081", "," & vbCrLf, "")
        Next

        Return lset

    End Function

    Private Function MakeDCSDataLayer(variable As String, season As String, rcp As String, year As String, lang As String) As String

        'calculate url (might be a constant)
        'tmean en/fr , tmin en/fr  , tmax en/fr  , prec en/fr
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=DCS.TX.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=fr&LAYERS=DCS.TX.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=DCS.TN.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=fr&LAYERS=DCS.TN.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=DCS.TM.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=fr&LAYERS=DCS.TM.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=DCS.PR.RCP85.FALL.2021-2040_PCTL50
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=fr&LAYERS=DCS.PR.RCP85.FALL.2021-2040_PCTL50

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "TX"}, {"tmin", "TN"}, {"tmax", "TM"}, {"prec", "PR"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "YEAR"}, {"MAM", "SPRING"}, {"JJA", "SUMMER"}, {"SON", "FALL"}, {"DJF", "WINTER"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim yearCode As String = year & "-" & CStr(CInt(year) + 19)
        Dim rcpCode As String = rcp.ToUpper()
        Dim template As String = "assets/templates/dcs/variables-template.html"
        Dim parser As String = "assets/templates/dcs/variables-script.js"

        Dim wmsCode As String = "DCS." & varCode & "." & rcpCode & "." & seasonCode & "." & yearCode & "_PCTL50"

        'derive unique layer id (ramp id)
        Dim rampID As String = "DCS_" & variable & "_" & season & "_" & rcp & "_" & year & "_" & lang

        Return MakeWMSLayerConfig(url, rampID, 1, False, wmsCode, oDCSLang.Txt(lang, LAYER_NAME, variable), "text/plain", template, parser)

    End Function

    Private Function MakeDCSLegend(variable As String, season As String, rcp As String, lang As String, rampid As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        'precip has different legend than temperature ones
        If variable = "prec" Then
            sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=DCS.PR.RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"
        Else
            sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=DCS.TX.RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"
        End If

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oDCSLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampid, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)

            'MakeUnboundLayerLegendBlockConfig(.Txt(lang, LAYER_NAME, variable), .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
        End With

        Return sLegend

    End Function

#End Region

#Region " AHCCD "

    ' WFS. No Time.

    ''' <summary>
    ''' Create set of config files for AHCCD
    ''' </summary>
    Private Sub MakeAHCCDConfigs()
        MakeAHCCDLang()

        For Each var As String In aAHCCDVar
            For Each season As String In aSeasonMonth
                Dim nugget As New LangNugget
                For Each lang As String In aLang
                    'derive unique layer id (ramp id)
                    Dim rampID As String = "AHCCD_" & var & "_" & season & "_" & lang

                    Dim dataLayers = MakeAHCCDDataLayer(var, season, lang, rampID)
                    Dim legund = MakeAHCCDLegend(var, season, lang, rampID)
                    Dim support = MakeSupportSet(lang, True, False, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("ahccd\1\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeAHCCDLang()
        Dim k As String 'lazy

        oAHCCDLang = New LangHive

        With oAHCCDLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short AHCCD dataset description goes here", "[fr] A short AHCCD dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Precipitation", "[fr] Precipitation", k)

            k = "supr"
            .AddItem(VAR_DESC, "A short surface pressure description goes here", "[fr] A short surface pressure description goes here", k)
            .AddItem(LAYER_NAME, "Surface pressure", "[fr] Surface pressure", k)

            k = "slpr"
            .AddItem(VAR_DESC, "A short sea level pressure description goes here", "[fr] A short sea level pressure description goes here", k)
            .AddItem(LAYER_NAME, "Sea level pressure", "[fr] Sea level pressure", k)

            k = "wind"
            .AddItem(VAR_DESC, "A short wind speed description goes here", "[fr] A short wind speed description goes here", k)
            .AddItem(LAYER_NAME, "Wind speed", "[fr] Wind speed", k)

            k = "red"
            .AddItem(LEGEND_TEXT, "Red Circle", "[fr] Red Circle", k)

            k = "green"
            .AddItem(LEGEND_TEXT, "Green Circle", "[fr] Green Circle", k)

            k = "blue"
            .AddItem(LEGEND_TEXT, "Blue Circle", "[fr] Blue Circle", k)

            'general non-colour one
            .AddItem(LEGEND_TEXT, "AHCCD Station", "[fr] AHCCD Station")

        End With

    End Sub

    Private Function MakeAHCCDDataLayer(variable As String, season As String, lang As String, rampId As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=temp_mean&period=\"Ann\"
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=temp_min
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=temp_max
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=total_precip
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=pressure_station
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=pressure_sea_level
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=wind_speed


        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "temp_mean"}, {"tmin", "temp_min"}, {"tmax", "temp_max"}, {"prec", "total_precip"}, {"supr", "pressure_station"}, {"slpr", "pressure_sea_level"}, {"wind", "wind_speed"}}

        'temp based ones are red. water based is blue. air based is green
        Dim dColour As New Dictionary(Of String, String) From {{"tmean", "#f04116"}, {"tmin", "#f04116"}, {"tmax", "#f04116"}, {"prec", "#0ca7f5"}, {"supr", "#0cf03a"}, {"slpr", "#0cf03a"}, {"wind", "#0cf03a"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "Ann"}, {"MAM", "Spr"}, {"JJA", "Smr"}, {"SON", "Fal"}, {"DJF", "Win"}, {"JAN", "Jan"}, {"FEB", "Feb"}, {"MAR", "Mar"}, {"APR", "Apr"}, {"MAY", "May"}, {"JUN", "Jun"}, {"JUL", "Jul"}, {"AUG", "Aug"}, {"SEP", "Sep"}, {"OCT", "Oct"}, {"NOV", "Nov"}, {"DEC", "Dec"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim colourCode As String = dColour.Item(variable)
        Dim template As String = "assets/templates/ahccd/variables-template.html"
        Dim parser As String = "assets/templates/ahccd/variables-script.js"

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=" & varCode & "&period=" & seasonCode

        Return MakeWFSLayerConfig(url, rampId, 1, True, "station_name", oAHCCDLang.Txt(lang, LAYER_NAME, variable), colourCode, template, parser)

    End Function

    Private Function MakeAHCCDLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""


        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"supr", "stnpress"}, {"slpr", "seapress"}, {"wind", "sfcwind"}}
        Dim dLegend As New Dictionary(Of String, String) From {{"tmean", "red"}, {"tmin", "red"}, {"tmax", "red"}, {"prec", "blue"}, {"supr", "green"}, {"slpr", "green"}, {"wind", "green"}}

        Dim sColour As String = dLegend.Item(variable)
        Dim sLegendUrl = "assets/images/" & sColour & "-circle.svg"

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oAHCCDLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, .Txt(lang, LEGEND_TEXT), 2,, "icons") &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " CAPA "

    ' WMS. Time (by hour).

    ''' <summary>
    ''' Create set of config files for CAPA
    ''' </summary>
    Private Sub MakeCAPAConfigs()

        MakeCAPALang()

        For Each var As String In aCAPAVar

            Dim nugget As New LangNugget
            For Each lang As String In aLang
                Dim dataLayers = MakeCAPAHourSet(var, lang)
                Dim legund = MakeCAPALegend(var, lang)
                Dim support = MakeSupportSet(lang, True, True, True)

                Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                nugget.setLang(lang, configstruct)
            Next

            Dim fileguts = MakeLangStructure(nugget)
            WriteConfig("capa\1\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Function MakeCAPARampID(variable As String, hour As String, lang As String) As String
        ' doing this due to the radio configuration of the legend.
        ' need to have set of ramp ids, not a singular
        Return "CAPA_" & variable & "_" & hour & "_" & lang
    End Function


    Private Sub MakeCAPALang()
        Dim k As String 'lazy

        oCAPALang = New LangHive

        With oCAPALang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short RDPA dataset description goes here", "[fr] A short RDPA dataset description goes here")

            k = "qp25"
            .AddItem(VAR_DESC, "A short Quantity of Precipitation, 2.5KM resolution description goes here", "[fr] A short Quantity of Precipitation, 2.5KM resolution description goes here", k)
            .AddItem(LAYER_NAME, "Quantity of Precipitation, 2.5KM resolution", "[fr] Quantity of Precipitation, 2.5KM resolution", k)

            k = "qp10"
            .AddItem(VAR_DESC, "A short Quantity of Precipitation, 10KM resolution description goes here", "[fr] A short Quantity of Precipitation, 10KM resolution description goes here", k)
            .AddItem(LAYER_NAME, "Quantity of Precipitation, 10KM resolution", "[fr] Quantity of Precipitation, 10KM resolution", k)

            .AddItem("CAPA_SLIDER", "Canadian Precipitation Analysis", "[fr] Canadian Precipitation Analysis")

            k = "24"
            .AddItem("CAPA_HOUR_DESC", "A short 24 hour description goes here", "[fr] A short 24 hour description goes here", k)

            k = "6"
            .AddItem("CAPA_HOUR_DESC", "A short 6 hour description goes here", "[fr] A short 6 hour description goes here", k)

        End With

    End Sub

    ''' <summary>
    ''' Makes the Data Layer array for CAPA "set of two time periods"
    ''' </summary>
    ''' <param name="variable"></param>
    ''' <param name="lang"></param>
    ''' <returns></returns>
    Private Function MakeCAPAHourSet(variable As String, lang As String) As String

        Dim lset As String = ""

        For Each hour As String In aHour
            lset = lset & MakeCAPADataLayer(variable, hour, lang) & IIf(hour <> "6", "," & vbCrLf, "")
        Next

        Return lset

    End Function

    Private Function MakeCAPADataLayer(variable As String, hour As String, lang As String) As String

        'calculate url (might be a constant)
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=HRDPA.6F 
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=HRDPA.24F
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=RDPA.6F 
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=RDPA.24F

        Dim url As String = "http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0"

        'TODO make global to prevent re-creating every iteration?
        'might need to add _PR to the key
        Dim dVari As New Dictionary(Of String, String) From {{"qp25", "HRDPA"}, {"qp10", "RDPA"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim wmsCode As String = varCode & "." & hour & "F_PR"

        'derive unique layer id (ramp id)
        Dim rampID As String = MakeCAPARampID(variable, hour, lang)
        Dim template As String = "assets/templates/capa/variables-template.html"
        Dim parser As String = "assets/templates/capa/variables-script.js"

        Return MakeWMSLayerConfig(url, rampID, 1, False, wmsCode, oCAPALang.Txt(lang, LAYER_NAME, variable) & " " & hour & "H", "text/plain", template, parser, True)

    End Function

    Private Function MakeCAPALegend(variable As String, lang As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        sLegendUrl = "http://geo.weather.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=HRDPA.6F_PR&format=image/png&STYLE=default"

        Dim dIcon As New Dictionary(Of String, String) From {{"6", "06h"}, {"24", "24h"}}

        Dim season As String

        'this can be cleaned up a lot after demo panic

        'special logic for exclusive visibility
        Dim oConNugget As New ConfigNugget(2)
        With oConNugget
            .AddLine("{")
            .AddLine("""name"": """ & oCAPALang.Txt(lang, "CAPA_SLIDER") & """,", 1)
            .AddLine("""expanded"": true,", 1)
            .AddLine("""controls"": [""visibility""],", 1)
            .AddLine("""children"": [{", 1)
            .AddLine("""exclusiveVisibility"": [", 2)

            season = "6"
            .AddRaw(MakeLayerLegendBlockConfig(
                    "",
                    MakeCAPARampID(variable, season, lang),
                    oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
                    "assets/images/" & dIcon.Item(season) & ".svg",
                    sLegendUrl, "", 5))


            season = "24"
            .AddRaw(MakeLayerLegendBlockConfig(
                    "",
                    MakeCAPARampID(variable, season, lang),
                    oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
                    "assets/images/" & dIcon.Item(season) & ".svg",
                    sLegendUrl, "", 5, False))

            .AddLine("]", 2)
            .AddLine("}]", 1)
            .AddLine("},")
        End With

        With oDCSLang

            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            oConNugget.Nugget &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Hydrometric "

    ' WFS. No Time.

    ''' <summary>
    ''' Create set of config files for Hydro
    ''' </summary>
    Private Sub MakeHydroConfigs()

        MakeHydroLang()

        Dim nugget As New LangNugget
        For Each lang As String In aLang

            'derive unique layer id (ramp id)
            Dim rampID As String = "Hydro_" & lang

            Dim dataLayers = MakeHydroDataLayer(lang, rampID)
            Dim legund = MakeHydroLegend(lang, rampID)
            Dim support = MakeSupportSet(lang, True, False, True)

            Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

            nugget.setLang(lang, configstruct)
        Next

        Dim fileguts = MakeLangStructure(nugget)
        WriteConfig("hydro\1\config-hydro.json", fileguts)
    End Sub

    Private Sub MakeHydroLang()

        oHydroLang = New LangHive

        'we only have one layer. so VAR_DESC might be redundant?

        With oHydroLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short Hydro dataset description goes here", "[fr] A short Hydro dataset description goes here")

            .AddItem(VAR_DESC, "A short hydro description goes here (maybe)", "[fr] A short hydro description goes here (maybe)")
            .AddItem(LAYER_NAME, "Hydrometric stations", "[fr] Hydrometric stations")

            .AddItem(LEGEND_TEXT, "Hydrometric Station", "[fr] Hydrometric Station")

        End With

    End Sub


    Private Function MakeHydroDataLayer(lang As String, rampID As String) As String

        'calculate url (might be a constant)
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=\%22Active\%22


        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=Active"

        Dim template As String = "assets/templates/hydro/stations-template.html"
        Dim parser As String = "assets/templates/hydro/stations-script.js"

        Return MakeWFSLayerConfig(url, rampID, 1, True, "STATION_NAME", oHydroLang.Txt(lang, LAYER_NAME), "#0cf03a", template, parser)

    End Function

    Private Function MakeHydroLegend(lang As String, rampID As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "" 'TODO needs to be supplied

        'TODO need a proper image
        Dim sCoverIcon = "assets/images/green-circle.svg"

        With oHydroLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampID, .Txt(lang, VAR_DESC), sCoverIcon, sLegendUrl, .Txt(lang, LEGEND_TEXT), 2,, "icons") &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " CanGRID "

    ' WMS. No Time.

    ''' <summary>
    ''' Create set of config files for CanGRID
    ''' </summary>
    Private Sub MakeCanGRIDConfigs()

        MakeCanGRIDLang()

        For Each var As String In aCanGRIDVar
            For Each season As String In aSeason ' aSeasonMonthly 'note different than SeasonMonth 'note asked to remove the "MONTHLY" part for now

                Dim nugget As New LangNugget
                For Each lang As String In aLang
                    'derive unique layer id (ramp id)
                    Dim rampID As String = "CanGRID_" & var & "_" & season & "_" & lang

                    Dim dataLayers = MakeCanGRIDDataLayer(var, season, lang, rampID)
                    Dim legund = MakeCanGRIDLegend(var, season, lang, rampID)
                    Dim support = MakeSupportSet(lang, True, True, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("CanGRD\1\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeCanGRIDLang()
        Dim k As String 'lazy

        oCanGRIDLang = New LangHive

        With oCanGRIDLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short CanGRID dataset description goes here", "[fr] A short CanGRID dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Precipitation", "[fr] Precipitation", k)

        End With

    End Sub

    Private Function MakeCanGRIDDataLayer(variable As String, season As String, lang As String, rampID As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en


        'TODO make global to prevent re-creating every iteration?
        'NOTE layer spreadsheet only indicates tmean and precip. will include other two codes if we add them.
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "TM"}, {"tmin", "TN"}, {"tmax", "TX"}, {"prec", "PR"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "ANNUAL"}, {"MAM", "SPRING"}, {"JJA", "SUMMER"}, {"SON", "FALL"}, {"DJF", "WINTER"}, {"MTH", "MONTHLY"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"
        Dim wmsCode As String = "CANGRID.TREND." & varCode & "_" & seasonCode
        Dim template As String = "assets/templates/cangrd/variables-template.html"
        Dim parser As String = "assets/templates/cangrd/variables-script.js"

        Return MakeWMSLayerConfig(url, rampID, 1, True, wmsCode, oCanGRIDLang.Txt(lang, LAYER_NAME, variable), "text/plain", template, parser)

    End Function

    Private Function MakeCanGRIDLegend(variable As String, season As String, lang As String, rampid As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        'precip has different legend than temperature ones
        If variable = "prec" Then
            sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANGRID.TREND.PR_ANNUAL&format=image/png&STYLE=default"
        Else
            sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANGRID.TREND.TM_ANNUAL&format=image/png&STYLE=default"
        End If

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oCanGRIDLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampid, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " CanSIPS "

    ' WMS. No Time.

    ''' <summary>
    ''' Create set of config files for CanSIPS
    ''' </summary>
    Private Sub MakeCanSIPSConfigs()

        MakeCanSIPSLang()

        Dim dVari As New Dictionary(Of String, String) From {{"slpr", "HIND.MEM.ETA_PN-SLP.10"}, {"itpr", "MEM.ETA_RT.10"}, {"stpr", "MEM.ETA_TT.10"},
            {"wtpr", "MEM.ETA_WTMP.10"}, {"gh5m", "PRES_GZ.500.10"}, {"ta8m", "PRES_TT.850.10"}, {"wd2m", "MEM.PRES_UU.200.10"},
            {"wd8m", "PRES_UU.850.10"}}

        For Each var As String In aCanSIPSVar

            Dim nugget As New LangNugget
            For Each lang As String In aLang
                'derive unique layer id (ramp id)
                Dim rampID As String = "CanSIPS_" & var & "_" & "_" & lang



                'calculate wms layer id.  easier to do it here
                Dim wmsCode As String = "CANSIPS." & dVari.Item(var)

                Dim dataLayers = MakeCanSIPSDataLayer(var, lang, rampID, wmsCode)
                Dim legund = MakeCanSIPSLegend(var, lang, rampID, wmsCode)
                Dim support = MakeSupportSet(lang, True, True, True)

                Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                nugget.setLang(lang, configstruct)
            Next

            Dim fileguts = MakeLangStructure(nugget)
            WriteConfig("cansips\1\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeCanSIPSLang()
        Dim k As String 'lazy

        oCanSIPSLang = New LangHive

        With oCanSIPSLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short CanSIPS dataset description goes here", "[fr] A short CanSIPS dataset description goes here")

            k = "slpr"
            .AddItem(VAR_DESC, "A short sea level pressure description goes here", "[fr] A short sea level pressure description goes here", k)
            .AddItem(LAYER_NAME, "Sea level pressure", "[fr] Sea level pressure", k)

            k = "itpr"
            .AddItem(VAR_DESC, "A short instantaneous precipitation rate description goes here", "[fr] A short instantaneous precipitation rate description goes here", k)
            .AddItem(LAYER_NAME, "Instantaneous precipitation rate", "[fr] Instantaneous precipitation rate", k)

            k = "stpr"
            .AddItem(VAR_DESC, "A short surface temperature description goes here", "[fr] A short surface temperature description goes here", k)
            .AddItem(LAYER_NAME, "Surface temperature", "[fr] Surface temperature", k)

            k = "wtpr"
            .AddItem(VAR_DESC, "A short water temperature description goes here", "[fr] A short water temperature description goes here", k)
            .AddItem(LAYER_NAME, "Water temperature", "[fr] Water temperature", k)

            k = "gh5m"
            .AddItem(VAR_DESC, "A short geopotential height at 500mb description goes here", "[fr] A short geopotential height at 500mb description goes here", k)
            .AddItem(LAYER_NAME, "Geopotential height at 500mb", "[fr] Geopotential height at 500mb", k)

            k = "ta8m"
            .AddItem(VAR_DESC, "A short temperature at 850mb description goes here", "[fr] A short temperature at 850mb description goes here", k)
            .AddItem(LAYER_NAME, "Temperature at 850mb", "[fr] Temperature at 850mb", k)

            k = "wd2m"
            .AddItem(VAR_DESC, "A short wind direction at 200mb description goes here", "[fr] A short wind direction at 200mb description goes here", k)
            .AddItem(LAYER_NAME, "Wind direction at 200mb", "[fr] Wind direction at 200mb", k)

            k = "wd8m"
            .AddItem(VAR_DESC, "A short wind direction at 850mb description goes here", "[fr] A short wind direction at 850mb description goes here", k)
            .AddItem(LAYER_NAME, "Wind direction at 850mb", "[fr] Wind direction at 850mb", k)

        End With

    End Sub

    Private Function MakeCanSIPSDataLayer(variable As String, lang As String, rampID As String, wmsID As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0"

        Return MakeWMSLayerConfig(url, rampID, 1, True, wmsID, oCanSIPSLang.Txt(lang, LAYER_NAME, variable), "text/plain")

    End Function

    Private Function MakeCanSIPSLegend(variable As String, lang As String, rampid As String, wmsID As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANSIPS.HIND.MEM.ETA_PN-SLP.10&format=image/png&STYLE=default
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANSIPS.MEM.ETA_RT.10&format=image/png&STYLE=default

        'precip has different legend than temperature ones

        sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=" & wmsID & "&format=image/png&STYLE=default"

        'TODO need proper icons
        Dim dIcon As New Dictionary(Of String, String) From {{"slpr", "HIND.MEM.ETA_PN-SLP.10"}, {"itpr", "MEM.ETA_RT.10"}, {"stpr", "MEM.ETA_TT.10"},
            {"wtpr", "MEM.ETA_WTMP.10"}, {"gh5m", "PRES_GZ.500.10"}, {"ta8m", "PRES_TT.850.10"}, {"wd2m", "MEM.PRES_UU.200.10"},
            {"wd8m", "PRES_UU.850.10"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oCanSIPSLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampid, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Daily "


    ' WFS. No Time.

    ''' <summary>
    ''' Create set of config files for Daily
    ''' </summary>
    Private Sub MakeDailyConfigs()
        MakeDailyLang()

        For Each var As String In aDailyVar
            Dim nugget As New LangNugget
            For Each lang As String In aLang
                'derive unique layer id (ramp id)
                Dim rampID As String = "Daily_" & var & "_" & lang

                Dim dataLayers = MakeDailyDataLayer(var, lang, rampID)
                Dim legund = MakeDailyLegend(var, lang, rampID)
                Dim support = MakeSupportSet(lang, True, False, True)

                Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                nugget.setLang(lang, configstruct)
            Next

            Dim fileguts = MakeLangStructure(nugget)
            WriteConfig("daily\1\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeDailyLang()
        Dim k As String 'lazy

        oDailyLang = New LangHive

        With oDailyLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short Daily dataset description goes here", "[fr] A short Daily dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Precipitation", "[fr] Precipitation", k)

        End With

    End Sub

    Private Function MakeDailyDataLayer(variable As String, lang As String, rampId As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec 
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-daily/items?TOTAL_PRECIPITATION_FLAG=T

        'TODO make global to prevent re-creating every iteration?
        'we encode the value field (used for maptip). layer url adds the "_FLAG" to enhance field name
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "MEAN_TEMPERATURE"}, {"tmin", "MIN_TEMPERATURE"}, {"tmax", "MAX_TEMPERATURE"}, {"prec", "TOTAL_PRECIPITATION"}}


        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-daily/items?" & varCode & "_FLAG=T"

        Return MakeWFSLayerConfig(url, rampId, 1, True, varCode, oDailyLang.Txt(lang, LAYER_NAME, variable), "#0cf03a")

    End Function

    Private Function MakeDailyLegend(variable As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "" 'TODO needs to be supplied

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oDailyLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Monthly "

    ' WFS. No Time.

    ''' <summary>
    ''' Create set of config files for Monthly
    ''' </summary>
    Private Sub MakeMonthlyConfigs()
        MakeMonthlyLang()

        For Each var As String In aMonthlyVar
            Dim nugget As New LangNugget
            For Each lang As String In aLang
                'derive unique layer id (ramp id)
                Dim rampID As String = "Monthly_" & var & "_" & lang

                Dim dataLayers = MakeMonthlyDataLayer(var, lang, rampID)
                Dim legund = MakeMonthlyLegend(var, lang, rampID)
                Dim support = MakeSupportSet(lang, True, False, True)

                Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                nugget.setLang(lang, configstruct)
            Next

            Dim fileguts = MakeLangStructure(nugget)
            WriteConfig("monthly\1\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeMonthlyLang()
        Dim k As String 'lazy

        oMonthlyLang = New LangHive

        With oMonthlyLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short Monthly dataset description goes here", "[fr] A short Monthly dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Precipitation", "[fr] Precipitation", k)

        End With

    End Sub

    Private Function MakeMonthlyDataLayer(variable As String, lang As String, rampId As String) As String

        'TODO need a ruling on the URL.  magic spreadsheet says to focus on a given year/month or do a time slider magic
        'hardcode for now

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec 
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-monthly/items?LOCAL_MONTH=12&LOCAL_YEAR=1981

        'TODO make global to prevent re-creating every iteration?       
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "MEAN_TEMPERATURE"}, {"tmin", "MIN_TEMPERATURE"}, {"tmax", "MAX_TEMPERATURE"}, {"prec", "TOTAL_PRECIPITATION"}}


        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-Monthly/items?LOCAL_MONTH=12&LOCAL_YEAR=1981"

        Return MakeWFSLayerConfig(url, rampId, 1, True, varCode, oMonthlyLang.Txt(lang, LAYER_NAME, variable), "#0cf03a")

    End Function

    Private Function MakeMonthlyLegend(variable As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "" 'TODO needs to be supplied

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oMonthlyLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Normals "

    ' WFS. No Time.

    ''' <summary>
    ''' Create set of config files for Normals
    ''' </summary>
    Private Sub MakeNormalsConfigs()
        MakeNormalsLang()

        For Each var As String In aNormalsVar
            For Each season As String In aSeasonMonthOnly

                Dim nugget As New LangNugget
                For Each lang As String In aLang

                    'derive unique layer id (ramp id)
                    Dim rampID As String = "Normals_" & var & "_" & season & "_" & lang

                    Dim dataLayers = MakeNormalsDataLayer(var, season, lang, rampID)
                    Dim legund = MakeNormalsLegend(var, season, lang, rampID)
                    Dim support = MakeSupportSet(lang, True, False, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("normal\1\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeNormalsLang()
        Dim k As String 'lazy

        oNormalsLang = New LangHive

        With oNormalsLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short Normals dataset description goes here", "[fr] A short Normals dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "A short mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean daily temperature", "[fr] Mean temperature", k)

            k = "tmin"
            .AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean daily minimum temperature", "[fr] Minimum temperature", k)

            k = "tmax"
            .AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean daily maximum temperature", "[fr] Maximum temperature", k)

            k = "prec"
            .AddItem(VAR_DESC, "A short precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Total precipitation", "[fr] Precipitation", k)

            'k = "stpr"
            '.AddItem(VAR_DESC, "A short station pressure description goes here", "[fr] A short surface pressure description goes here", k)
            '.AddItem(LAYER_NAME, "Average station pressure", "[fr] Station pressure", k)

            'k = "slpr"
            '.AddItem(VAR_DESC, "A short sea level pressure description goes here", "[fr] A short sea level pressure description goes here", k)
            '.AddItem(LAYER_NAME, "Average sea level pressure", "[fr] Sea level pressure", k)

            'k = "wind"
            '.AddItem(VAR_DESC, "A short wind speed description goes here", "[fr] A short wind speed description goes here", k)
            '.AddItem(LAYER_NAME, "Wind speed", "[fr] Wind speed", k)

            'k = "mgst"
            '.AddItem(VAR_DESC, "A short Maximum gust speed description goes here", "[fr] A short Maximum Gust Speed description goes here", k)
            '.AddItem(LAYER_NAME, "Maximum gust speed", "[fr] Maximum Gust Speed", k)

            'k = "dgst"
            '.AddItem(VAR_DESC, "A short Direction of Maximum Gust description goes here", "[fr] A short Direction of Maximum Gust description goes here", k)
            '.AddItem(LAYER_NAME, "Direction of maximum gust", "[fr] Direction of Maximum Gust", k)

        End With

    End Sub


    Private Function MakeNormalsDataLayer(variable As String, season As String, lang As String, rampId As String) As String

        'TODO layer is currently down.  still need to see how data/service is structured

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "1"}, {"tmin", "8"}, {"tmax", "5"}, {"prec", "56"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "13"}, {"JAN", "1"}, {"FEB", "2"}, {"MAR", "3"}, {"APR", "4"}, {"MAY", "5"}, {"JUN", "6"}, {"JUL", "7"}, {"AUG", "8"}, {"SEP", "9"}, {"OCT", "10"}, {"NOV", "11"}, {"DEC", "12"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-normals/items?NORMAL_ID=" & varCode & "&MONTH=" & seasonCode

        Return MakeWFSLayerConfig(url, rampId, 1, True, "ID", oNormalsLang.Txt(lang, LAYER_NAME, variable), "#0cf03a")

    End Function

    Private Function MakeNormalsLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = ""

        'TODO update icons
        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oNormalsLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region


End Class
