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
    Dim aCAPAVar = {"qp25", "qp10"}
    Dim aCMIP5Var = {"snow", "sith", "sico", "wind"}
    Dim aDailyVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aDCSVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aMonthlyVar = {"tmean", "tmin", "tmax", "prec"}
    Dim aNormalsVar = {"tmean", "tmin", "tmax", "prec", "stpr", "slpr", "wind", "mgst", "dgst"}

    Dim aSeason = {"ANN", "MAM", "JJA", "SON", "DJF"}
    Dim aSeasonMonth = {"ANN", "MAM", "JJA", "SON", "DJF", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"}
    Dim aSeasonMonthly = {"ANN", "MAM", "JJA", "SON", "DJF", "MTH"}
    Dim aYear = {"2021", "2041", "2061", "2081"}
    Dim aHour = {"24", "6"} 'want this order on time slider

    'language hives. global scope for sharing fun
    Dim oCommonLang As LangHive
    Dim oAHCCDLang As LangHive
    Dim oCanGRIDLang As LangHive
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

    Private Sub cmdEnhanceMini_Click(sender As Object, e As EventArgs) Handles cmdEnhanceMini.Click

        'MAIN STARTING POINT OF APP.
        MakeCommonLang()

        'MakeCMIP5Configs()
        MakeDCSConfigs()
        MakeAHCCDConfigs()
        MakeCAPAConfigs()
        MakeHydroConfigs()
        MakeCanGRIDConfigs()
        MakeDailyConfigs()
        MakeMonthlyConfigs()
        MakeNormalsConfigs()

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
                                        Optional template As String = "", Optional parser As String = "") As String
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
        nugget.AddLine("""url"": """ & url & """,", 1)
        nugget.AddLine("""name"": """ & layerName & """,", 1)
        nugget.AddLine("""state"": {", 1)
        nugget.AddLine("""opacity"": " & opacity & ",", 2)
        nugget.AddLine("""visibility"": " & BoolToJson(visible), 2)
        nugget.AddLine("},", 1)
        nugget.AddLine("""layerEntries"": [{""id"": """ & wmsLayerId & """ }],", 1)
        InjectTemplate(nugget, 1, template, parser)
        nugget.AddLine("""controls"": [""data""]", 1)
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
                                          Optional template As String = "", Optional parser As String = "") As String
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
          legendImg As String, legendText As String, indentLevel As Integer, Optional trailingComma As Boolean = True) As String


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
        nugget.AddLine("""symbologyRenderStyle"": ""images""", 1)
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
            {"NOV", "nov"}, {"DEC", "dec"}}

        Return dSeason.Item(season)

    End Function


    Private Function FileVar(var As String) As String

        'TODO assuming common keys across similar vars (e.g. tmax is always same key).
        'restructure if not the case

        '"tmean", "tmin", "tmax", "prec", "supr", "slpr", "wind"
        Dim dVar As New Dictionary(Of String, String) From {{"wind", "sfcwind"}, {"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"supr", "stnpress"},
            {"slpr", "seapress"}, {"qp25", "hrdpa"}, {"qp10", "rdpa"}, {"MAY", "may"}, {"JUN", "jun"}, {"JUL", "jul"}, {"AUG", "aug"}, {"SEP", "sep"}, {"OCT", "oct"},
            {"NOV", "nov"}, {"DEC", "dec"}}

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
                                   PROVINCES_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, PROVINCES_LAYER_ID))
    End Function

    Private Function MakeCitiesConfig(lang As String) As String

        Return MakeTileLayerConfig("http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/Overlays/Cities/MapServer",
                                   CITIES_LAYER_ID, 1, False, oCommonLang.Txt(lang, LAYER_NAME, CITIES_LAYER_ID))
    End Function

    Private Function MakeLabelsConfig(lang As String) As String

        Return MakeTileLayerConfig("http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBMT_TXT_3978/MapServer",
                                   LABELS_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, LABELS_LAYER_ID))
    End Function

#End Region

#Region " CMIP5 "

    'TODO needs revist / fixin

    Private Sub MakeCMIP5Configs()
        For Each var As String In aCMIP5Var
            For Each season As String In aSeason
                For Each rcp As String In aRcp
                    Dim nugget As New LangNugget
                    For Each lang As String In aLang
                        Dim dataLayers = MakeCMIP5YearSet(var, season, rcp, lang)
                        Dim legund = MakeCMIP5Legend(var, season, rcp, lang)
                        Dim support = MakeSupportSet(lang, True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)

                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("testcmip5_" & var & season & rcp & ".json", fileguts)

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
    Private Function MakeCMIP5YearSet(variable As String, season As String, rcp As String, lang As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeCMIP5DataLayer(variable, season, rcp, year, lang) & IIf(year <> "2081", ",", "") & vbCrLf
        Next

        Return lset

    End Function

    Private Function MakeCMIP5DataLayer(variable As String, season As String, rcp As String, year As String, lang As String) As String

        'calculate url (might be a constant)
        'calculate wms layer id
        'derive unique layer id (ramp id)

        Return MakeWMSLayerConfig("url", "id", 1, True, "wmslayer", "layer name")

    End Function

    Private Function MakeCMIP5Legend(variable As String, season As String, rcp As String, lang As String) As String

        Return "{ ""legend"": true }"

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
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

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

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=" & lang

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "TX"}, {"tmin", "TN"}, {"tmax", "TM"}, {"prec", "PR"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "YEAR"}, {"MAM", "SPRING"}, {"JJA", "SUMMER"}, {"SON", "FALL"}, {"DJF", "WINTER"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim yearCode As String = year & "-" & CStr(CInt(year) + 19)
        Dim rcpCode As String = rcp.ToUpper()

        Dim wmsCode As String = "DCS." & varCode & "." & rcpCode & "." & seasonCode & "." & yearCode & "_PCTL50"

        'derive unique layer id (ramp id)
        Dim rampID As String = "DCS_" & variable & "_" & season & "_" & rcp & "_" & year & "_" & lang

        Return MakeWMSLayerConfig(url, rampID, 1, False, wmsCode, oDCSLang.Txt(lang, LAYER_NAME, variable))

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
                    Dim support = MakeSupportSet(lang, True, True, True)

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

        End With

    End Sub

    Private Function MakeAHCCDDataLayer(variable As String, season As String, lang As String, rampId As String) As String
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

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

        Return MakeWFSLayerConfig(url, rampId, 1, True, "trend_value", oAHCCDLang.Txt(lang, LAYER_NAME, variable), colourCode, template, parser)

    End Function

    Private Function MakeAHCCDLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=AHCCD.STATIONS&format=image/png&STYLE=default"

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"supr", "stnpress"}, {"slpr", "seapress"}, {"wind", "sfcwind"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oAHCCDLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
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
            .AddItem(TOP_DESC, "A short CAPA dataset description goes here", "[fr] A short DCS dataset description goes here")

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
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

        'calculate url (might be a constant)
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=HRDPA.6F 
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=HRDPA.24F
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=RDPA.6F 
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=RDPA.24F

        Dim url As String = "http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=" & lang

        'TODO make global to prevent re-creating every iteration?
        'might need to add _PR to the key
        Dim dVari As New Dictionary(Of String, String) From {{"qp25", "HRDPA_PR"}, {"qp10", "RDPA_PR"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim wmsCode As String = varCode & "." & hour & "F_PR"

        'derive unique layer id (ramp id)
        Dim rampID As String = MakeCAPARampID(variable, hour, lang)

        Return MakeWMSLayerConfig(url, rampID, 1, False, wmsCode, oCAPALang.Txt(lang, LAYER_NAME, variable) & " " & hour & "H")

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
            .AddLine("""name"": """ & oCAPALang.Txt(lang, "CAPA_SLIDER") & """,")
            .AddLine("""expanded"": true,")
            .AddLine("""controls"": [""visibility""],")
            .AddLine("""children"": [{")
            .AddLine("""exclusiveVisibility"": [", 1)

            season = "6"
            .AddRaw(MakeLayerLegendBlockConfig(
                    "",
                    MakeCAPARampID(variable, season, lang),
                    oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
                    "assets/images/" & dIcon.Item(season) & ".svg",
                    sLegendUrl, "", 4))


            season = "24"
            .AddRaw(MakeLayerLegendBlockConfig(
                    "",
                    MakeCAPARampID(variable, season, lang),
                    oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
                    "assets/images/" & dIcon.Item(season) & ".svg",
                    sLegendUrl, "", 4, False))

            .AddLine("]", 1)
            .AddLine("}]")
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
            Dim support = MakeSupportSet(lang, True, True, True)

            Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

            nugget.setLang(lang, configstruct)
        Next

        Dim fileguts = MakeLangStructure(nugget)
        WriteConfig("testHydro.json", fileguts)
    End Sub

    Private Sub MakeHydroLang()

        oHydroLang = New LangHive

        'we only have one layer. so VAR_DESC might be redundant?

        With oHydroLang
            .AddItem(TOP_TITLE, "Data", "[fr] Data")
            .AddItem(TOP_DESC, "A short Hydro dataset description goes here", "[fr] A short Hydro dataset description goes here")

            .AddItem(VAR_DESC, "A short hydro description goes here (maybe)", "[fr] A short hydro description goes here (maybe)")
            .AddItem(LAYER_NAME, "Hydrometric stations", "[fr] Hydrometric stations")

        End With

    End Sub


    Private Function MakeHydroDataLayer(lang As String, rampID As String) As String
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

        'calculate url (might be a constant)
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=\%22Active\%22


        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=Active"

        Return MakeWFSLayerConfig(url, rampID, 1, True, "STATION_NAME", oHydroLang.Txt(lang, LAYER_NAME), "#0cf03a")

    End Function

    Private Function MakeHydroLegend(lang As String, rampID As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "" 'TODO needs to be supplied

        'TODO need a proper image
        Dim sCoverIcon = "assets/images/happy.svg"

        With oHydroLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampID, .Txt(lang, VAR_DESC), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
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
        For Each var As String In aCanGRIDVar
            For Each season As String In aSeasonMonthly 'note different than SeasonMonth

                Dim nugget As New LangNugget
                For Each lang As String In aLang
                    Dim dataLayers = MakeCanGRIDDataLayer(var, season, lang)
                    Dim legund = MakeCanGRIDLegend(var, season, lang)
                    Dim support = MakeSupportSet(lang, True, True, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("testCanGRID_" & var & season & ".json", fileguts)
            Next
        Next
    End Sub


    Private Function MakeCanGRIDDataLayer(variable As String, season As String, lang As String) As String
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en


        'TODO make global to prevent re-creating every iteration?
        'NOTE layer spreadsheet only indicates tmean and precip. will include other two codes if we add them.
        '     and ??? at why TX is min and TM is max
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "TN"}, {"tmin", "TX"}, {"tmax", "TM"}, {"prec", "PR"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "ANNUAL"}, {"MAM", "SPRING"}, {"JJA", "SUMMER"}, {"SON", "FALL"}, {"DJF", "WINTER"}, {"MTH", "MONTHLY"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)

        Dim url As String = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=" & lang

        Dim wmsCode As String = "CANGRID.TREND." & varCode & "_" & seasonCode

        'derive unique layer id (ramp id)
        Dim rampID As String = "CanGRID_" & variable & "_" & season & "_" & lang

        Return MakeWMSLayerConfig(url, rampID, 1, True, wmsCode, "LAYER NAME HERE")

    End Function

    Private Function MakeCanGRIDLegend(variable As String, season As String, lang As String) As String

        Return "{ ""legend"": true }"

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
                Dim support = MakeSupportSet(lang, True, True, True)

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
            MakeLegendSettingsConfig(lang, True, True, True)
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
                Dim support = MakeSupportSet(lang, True, True, True)

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
            MakeLegendSettingsConfig(lang, True, True, True)
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
            For Each season As String In aSeasonMonth 'TODO review that this is accurate array

                Dim nugget As New LangNugget
                For Each lang As String In aLang

                    'derive unique layer id (ramp id)
                    Dim rampID As String = "Normals_" & var & "_" & season & "_" & lang

                    Dim dataLayers = MakeNormalsDataLayer(var, season, lang, rampID)
                    Dim legund = MakeNormalsLegend(var, season, lang, rampID)
                    Dim support = MakeSupportSet(lang, True, True, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("testNormals_" & var & season & ".json", fileguts)
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

            k = "stpr"
            .AddItem(VAR_DESC, "A short station pressure description goes here", "[fr] A short surface pressure description goes here", k)
            .AddItem(LAYER_NAME, "Average station pressure", "[fr] Station pressure", k)

            k = "slpr"
            .AddItem(VAR_DESC, "A short sea level pressure description goes here", "[fr] A short sea level pressure description goes here", k)
            .AddItem(LAYER_NAME, "Average sea level pressure", "[fr] Sea level pressure", k)

            k = "wind"
            .AddItem(VAR_DESC, "A short wind speed description goes here", "[fr] A short wind speed description goes here", k)
            .AddItem(LAYER_NAME, "Wind speed", "[fr] Wind speed", k)

            k = "mgst"
            .AddItem(VAR_DESC, "A short Maximum gust speed description goes here", "[fr] A short Maximum Gust Speed description goes here", k)
            .AddItem(LAYER_NAME, "Maximum gust speed", "[fr] Maximum Gust Speed", k)

            k = "dgst"
            .AddItem(VAR_DESC, "A short Direction of Maximum Gust description goes here", "[fr] A short Direction of Maximum Gust description goes here", k)
            .AddItem(LAYER_NAME, "Direction of maximum gust", "[fr] Direction of Maximum Gust", k)

        End With

    End Sub


    Private Function MakeNormalsDataLayer(variable As String, season As String, lang As String, rampId As String) As String

        'TODO layer is currently down.  still need to see how data/service is structured

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "temp_mean"}, {"tmin", "temp_min"}, {"tmax", "temp_max"}, {"prec", "total_precip"}, {"stpr", "pressure_station"}, {"slpr", "pressure_sea_level"}, {"wind", "wind_speed"}, {"mgst", "???"}, {"dgst", "???"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "Ann"}, {"MAM", "Spr"}, {"JJA", "Smr"}, {"SON", "Fal"}, {"DJF", "Win"}, {"JAN", "Jan"}, {"FEB", "Feb"}, {"MAR", "Mar"}, {"APR", "Apr"}, {"MAY", "May"}, {"JUN", "Jun"}, {"JUL", "Jul"}, {"AUG", "Aug"}, {"SEP", "Sep"}, {"OCT", "Oct"}, {"NOV", "Nov"}, {"DEC", "Dec"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/Normals-trends/items?"

        Return MakeWFSLayerConfig(url, rampId, 1, True, "DISPLAY FIELD ???", oNormalsLang.Txt(lang, LAYER_NAME, variable), "#0cf03a")

    End Function

    Private Function MakeNormalsLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=Normals.STATIONS&format=image/png&STYLE=default"

        'TODO update icons
        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"stpr", "stnpress"}, {"slpr", "seapress"}, {"wind", "sfcwind"}, {"mgst", "???"}, {"dgst", "???"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".svg"

        With oNormalsLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

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

    Private Function MakeLayerSet(variable As String, subPeroid As String, rcp As String) As String

        Dim lset As String = ""

        For Each year As String In aYear
            lset = lset & MakeLayerSnippet(variable, subPeroid, rcp, year, year <> "2081")
        Next

        Return lset

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

#End Region

End Class
