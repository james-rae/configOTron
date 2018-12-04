Imports System.IO


Public Class ConfigForm

    'Config-o-tron for dummies:

    'SETUP
    'Check the DUMP_FOLDER variable, point it where you want output to go.
    'I'm lazy so make sure you add the appropriate subfolders to your dump folder.  Get a student to do it. Or copy the config directory from CCP viewer, as it has matching folders.
    'The MINIFY var (in ConfigNugget.vb) will eliminate most of the padding spaces and a good chunk of hard returns. It's not perfect but for lazy effort it gets most of the way there.
    'The APP_CONFIGS var (near bottom of file, use Ctrl-F to find) will set the copybot target location.

    'USAGE
    'The main Enhance button will generate configs in the dump directory. Say "enhance" when you press it, this will help avoid errors.
    'The Copybot button will copy stuff from the dump folder to wherever your CCP viewer configs are lurking. Use this if you don't want to do it by hand.
    'The Language Dump will take all the language strings being used and export them to a tab delimited text file in the DUMP_FOLDER.
    'The dropdown environment combo (currently has DEV and PROD) will influence what versions of configs get generated (mainly this affects service URLs)
    'and where things get copied to.  DEV creates configs with dev services, PROD creates configs with environment placeholders (multi-config).

    'NOTES
    'Lots of cut & paste, boilerplate, redundant code structures here. While it looks messy, it's done on purpose.
    'The amount of pivots and special requests to configs means having a very nice code-reuse structure is at risk of being ruined by one change request.
    'Having individual functions for every variable means we can adjust one without impacting the others. 
    'Some very inefficient code (e.g. re-creating lookup dictionaries for every file), but since this runs in 1 second, it's fairly irrelevant.
    'Note that Daily/Monthly/Stations configs are not being maintained in this project. They are managed by hand in the main CCP viewer project.

    Const DUMP_FOLDER As String = "c:\git\configotron\configotron\dump\"
    Dim ENV As String = "DEV"

    ' arrays of domains. global scope for sharing fun
    Dim aLang = {"en", "fr"}
    Dim aRcp = {"rcp26", "rcp45", "rcp85"}
    Dim aAHCCDVar = {"tmean", "tmin", "tmax", "prec", "supr", "slpr", "wind"}
    Dim aCanGRIDVar = {"tmean", "prec"} ' "tmin", "tmax",
    Dim aCanSIPSVar = {"slpr", "itpr", "stpr", "wtpr", "gh5m", "ta8m", "wd2m", "wd8m"}
    Dim aCAPAVar = {"qp10"}  ' "qp25",
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
    Const COLUMN_NAME As String = "ColumnName"

    'url replacement keys
    Const GEOMET_WMS As String = "#GEOMETWMS#"
    Const GEOMET_WFS As String = "#GEOMETWFS#"
    Const GEOMET_CLIMATE_WMS As String = "#GEOMETCLIMATEWMS#"
    Const ECCC_ARCGIS As String = "#ECCCARCGIS#"

    Dim oLangParty As New List(Of String)

    Private Sub cmdEnhanceMini_Click(sender As Object, e As EventArgs) Handles cmdEnhanceMini.Click

        ENV = cboEnv.Text.Trim

        'MAIN STARTING POINT OF APP.
        MakeCommonLang()

        MakeCMIP5Configs()     'M WMS Time Slider
        MakeDCSConfigs()       'M WMS Time Slider
        MakeAHCCDConfigs()     'F WFS
        MakeCAPAConfigs()      'M WMS Time Radio
        MakeHydroConfigs()     'F WFS
        MakeCanGRIDConfigs()   'M WMS
        MakeNormalsConfigs()   'F WFS

        ' not up to date
        ' MakeDailyConfigs()     'F WFS
        ' MakeMonthlyConfigs()   'F WFS

        ' removed
        ' MakeCanSIPSConfigs()   'M WMS Fancy Slider

        MsgBox("DONE THANKS")
    End Sub



#Region " General Structure Builders "

    Private Sub MakeCommonLang()
        oCommonLang = New LangHive("COMMON", oLangParty)
        With oCommonLang
            'support layer names
            .AddItem(LAYER_NAME, "Labels", "Étiquettes", LABELS_LAYER_ID)
            .AddItem(LAYER_NAME, "Provinces", "Provinces", PROVINCES_LAYER_ID)
            .AddItem(LAYER_NAME, "Cities", "Villes", CITIES_LAYER_ID)
            .AddItem(SETTINGS_TITLE, "Settings", "Paramètres")
        End With
    End Sub

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
        nugget.AddLine("""controls"": [" & IIf(visibleToggle, """visibility"", ""opacity"", ""settings""", "") & "]", 1)
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
        nugget.AddLine("""controls"": [""visibility"", ""opacity"", ""settings""],", 1)
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

    ''' <summary>
    ''' will insert a grid section to a layer config
    ''' the sourceNugget contents are modified by this function.
    ''' We assume its not the last item in the layer object, due to lazyness
    ''' </summary>
    ''' <param name="sourceNugget"></param>
    ''' <param name="startingLevel"></param>
    ''' <param name="columnArray">A 2-d array, with n items on first dimension (an item per grid column) and 3 items on second dimension (data, name, visibility)</param>
    Private Sub InjectGrid(sourceNugget As ConfigNugget, startingLevel As Integer, columnArray As Object)

        If columnArray Is Nothing Then
            Exit Sub
        End If

        Dim numberOfCols = columnArray.getUpperBound(0)

        If numberOfCols = 0 Then
            Exit Sub
        End If

        sourceNugget.AddLine("""table"": {", startingLevel)
        sourceNugget.AddLine("""columns"": [", startingLevel + 1)
        For colNum = 0 To numberOfCols
            sourceNugget.AddLine("{ ""data"": """ & columnArray(colNum, 0) & """" &
                           IIf(columnArray(colNum, 2), ", ""title"": """ & columnArray(colNum, 1) & """", "") &
                           ", ""visible"": " & columnArray(colNum, 2) &
                           " }" & IIf(colNum = numberOfCols, "", ","), startingLevel + 2)
        Next
        sourceNugget.AddLine("]", startingLevel + 1)
        sourceNugget.AddLine("},", startingLevel)


    End Sub

    Private Function MakeWFSLayerConfig(url As String, id As String, opacity As Double, visible As Boolean, nameField As String, layerName As String, colour As String,
                                        Optional template As String = "", Optional parser As String = "", Optional gridColArray As Object = Nothing) As String
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
        nugget.AddLine("""xyInAttribs"": true,", 1)
        nugget.AddLine("""url"": """ & url & """,", 1)
        nugget.AddLine("""name"": """ & layerName & """,", 1)
        nugget.AddLine("""nameField"": """ & nameField & """,", 1)
        nugget.AddLine("""state"": {", 1)
        nugget.AddLine("""opacity"": " & opacity & ",", 2)
        nugget.AddLine("""visibility"": " & BoolToJson(visible), 2)
        nugget.AddLine("},", 1)
        InjectTemplate(nugget, 1, template, parser)
        InjectGrid(nugget, 1, gridColArray)
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
    Private Function MakeSimpleLegendBlockConfig(infoType As String, content As String, indentLevel As Integer, Optional trailingComma As Boolean = True, Optional noExport As Boolean = False) As String

        Dim nugget As New ConfigNugget(indentLevel)

        nugget.AddLine("{")
        If noExport Then
            nugget.AddLine("""export"": false,", 1)
        End If
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

    Private Function MakeHiddenExclusiveLegendBlockConfig(children As String, indentLevel As Integer) As String

        'special logic for exclusive visibility
        Dim oConNugget As New ConfigNugget(indentLevel)
        With oConNugget
            .AddLine("{")

            .AddLine("""collapse"": true,", 1)
            .AddLine("""exclusiveVisibility"": [", 1)

            .AddRaw(children)

            .AddLine("]", 1)
            .AddLine("},")
        End With

        Return oConNugget.Nugget

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

        Dim json As String = MakeSimpleLegendBlockConfig("title", titleText, 2,, True) &
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
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, CITIES_LAYER_ID), CITIES_LAYER_ID, "assets/images/cities.png", padLevel, (prov Or labels))
        End If

        If labels Then
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, LABELS_LAYER_ID), LABELS_LAYER_ID, "assets/images/labels.png", padLevel, prov)
        End If

        If prov Then
            json &= MakeOverlayLegendBlockConfig(oCommonLang.Txt(lang, LAYER_NAME, PROVINCES_LAYER_ID), PROVINCES_LAYER_ID, "assets/images/provinces.png", padLevel, False)
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
            {"slpr", "seapress"}, {"qp25", "hrdpa"}, {"qp10", "rdpa"}, {"snow", "snd"}, {"sith", "sit"}, {"sico", "sic"}, {"itpr", "precip"}, {"stpr", "tsurface"},
            {"wtpr", "twater"}, {"gh5m", "geopotential"}, {"ta8m", "tmean"}, {"wd2m", "gustdir"}, {"wd8m", "gustdir850"}}

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

        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "https://maps-cartes.dev.ec.gc.ca/arcgis/rest/services/Overlays/Provinces/MapServer"},
            {"PROD", ECCC_ARCGIS & "/Overlays/Provinces/MapServer"}}

        Return MakeTileLayerConfig(dUrl.Item(ENV), PROVINCES_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, PROVINCES_LAYER_ID),,, "10000")

    End Function

    Private Function MakeCitiesConfig(lang As String) As String

        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "https://maps-cartes.dev.ec.gc.ca/arcgis/rest/services/Overlays/Cities/MapServer"},
            {"PROD", ECCC_ARCGIS & "/Overlays/Cities/MapServer"}}

        Return MakeTileLayerConfig(dUrl.Item(ENV), CITIES_LAYER_ID, 1, False, oCommonLang.Txt(lang, LAYER_NAME, CITIES_LAYER_ID),,, "10000")

    End Function

    Private Function MakeLabelsConfig(lang As String) As String
        Dim url As String = "https://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/" & IIf(lang = "en", "CBMT", "CBCT") & "_TXT_3978/MapServer"
        Return MakeTileLayerConfig(url, LABELS_LAYER_ID, 1, True, oCommonLang.Txt(lang, LAYER_NAME, LABELS_LAYER_ID),,, "10000")

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


                        Dim dataLayers = MakeCMIP5YearSet(var, season, rcp, lang)
                        Dim legund = MakeCMIP5Legend(var, season, rcp, lang)
                        Dim support = MakeSupportSet(lang, True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)
                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("CMIP5\config-" & FileVar(var) & "-" & FileSeason(season) & "-" & rcp & ".json", fileguts)

                Next
            Next
        Next
    End Sub

    Private Sub MakeCMIP5Lang()
        Dim k As String 'lazy

        oCMIP5Lang = New LangHive("CMIP5", oLangParty)

        With oCMIP5Lang
            .AddItem(TOP_TITLE, "Data", "Données")
            ' .AddItem(TOP_DESC, "A short CMIP5 dataset description goes here", "[fr] A short CMIP5 dataset description goes here")

            k = "snow"
            .AddItem(VAR_DESC, "Projected changes in snow depth are with respect to the reference period of 1986-2005 and expressed as percentage change (%).",
                     "Les changements projetés dans l'épaisseur de la neige sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Snow depth", "Épaisseur de la neige", k)

            k = "sith"
            .AddItem(VAR_DESC, "Projected changes in sea ice thickness are with respect to the reference period of 1986-2006 and expressed as percentage change (%).",
                     "Les changements projetés dans l'épaisseur de la glace de mer sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2006.", k)
            .AddItem(LAYER_NAME, "Sea ice thickness", "Épaisseur de la glace de mer", k)

            k = "sico"
            .AddItem(VAR_DESC, "Projected changes in sea ice concentration are with respect to the reference period of 1986-2005 and expressed as percentage change (%). Sea ice concentration is represented as the percentage (%) of grid cell area.",
                     "Les changements projetés dans la concentration de la glace de mer sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2005. La concentration de la glace de mer est exprimée en pourcentage (%) de la surface.", k)
            .AddItem(LAYER_NAME, "Sea ice concentration", "Concentration de la glace de mer", k)

            k = "wind"
            .AddItem(VAR_DESC, "Projected changes in wind speed are with respect to the reference period of 1986-2005 and expressed as percentage change (%).",
                     "Les changements projetés dans la vitesse du vent sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Surface wind speed", "Vitesse du vent à la surface", k)

            k = "tmean"
            .AddItem(VAR_DESC, "Projected changes in mean temperature (°C) are with respect to the reference period of 1986-2005.",
                     "Les changements projetés dans la température moyenne (°C) sont calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Mean temperature", "Température moyenne", k)

            k = "prec"
            .AddItem(VAR_DESC, "Projected relative changes in mean precipitation are with respect to the reference period of 1986-2005 and expressed as percentage change (%).",
                     "Les changements projetés dans les précipitations moyennes sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Mean precipitation", "Précipitations moyennes", k)

        End With

    End Sub

    Private Function MakeCMIP5RampId(variable As String, season As String, rcp As String, year As String, lang As String) As String
        Return "CMIP5_" & variable & "_" & season & "_" & rcp & "_" & year & "_" & lang
    End Function

    Private Function MakeCMIP5DataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"},
            {"PROD", GEOMET_CLIMATE_WMS & "?SERVICE=WMS&VERSION=1.3.0"}}

        Return dUrl.Item(ENV)

    End Function

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
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0&lang=en&LAYERS=CMIP5.SND.RCP26.ANNUAL.2081-2100_PCTL50

        Dim url As String = MakeCMIP5DataUrl()

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"snow", "SND"}, {"sith", "SIT"}, {"sico", "SIC"}, {"wind", "SFCWIND"}, {"tmean", "TT"}, {"prec", "PR"}}
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
        Dim rampID As String = MakeCMIP5RampId(variable, season, rcp, year, lang)

        Return MakeWMSLayerConfig(url, rampID, 0.85, False, wmsCode, oCMIP5Lang.Txt(lang, LAYER_NAME, variable), "application/json", template, parser, True)

    End Function

    Private Function MakeCMIP5Legend(variable As String, season As String, rcp As String, lang As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        Dim dVari As New Dictionary(Of String, String) From {{"snow", "SND"}, {"sith", "SIT"}, {"sico", "SIC"}, {"wind", "SFCWIND"}, {"tmean", "TT"}, {"prec", "PR"}}
        Dim dIcon As New Dictionary(Of String, String) From {{"snow", "snd"}, {"sith", "sit"}, {"sico", "sic"}, {"wind", "sfcwind"}, {"tmean", "tmean"}, {"prec", "precip"}}

        sLegendUrl = MakeCMIP5DataUrl() & "&request=GetLegendGraphic&sld_version=1.1.0&layer=CMIP5." & dVari.Item(variable) & ".RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"


        With oCMIP5Lang
            Dim lset As String = ""

            For Each year As String In aYear
                Dim rampId As String = MakeCMIP5RampId(variable, season, rcp, year, lang)
                lset &= MakeLayerLegendBlockConfig("", rampId, "", sCoverIcon, sLegendUrl, "", 2, year <> "2081")
            Next

            'note: putting variable text in the data block instead of in lower legend
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, VAR_DESC, variable)) &
            MakeHiddenExclusiveLegendBlockConfig(lset, 2) &
            MakeLegendSettingsConfig(lang, True, True, True)

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

                        Dim dataLayers = MakeDCSYearSet(var, season, rcp, lang)  ' TODO we may need to add a 5th year period for "historical"
                        Dim legund = MakeDCSLegend(var, season, rcp, lang)
                        Dim support = MakeSupportSet(lang, True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)
                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("dcs\config-" & FileVar(var) & "-" & FileSeason(season) & "-" & rcp & ".json", fileguts)

                Next
            Next
        Next
    End Sub

    Private Sub MakeDCSLang()
        Dim k As String 'lazy

        oDCSLang = New LangHive("DCS", oLangParty)

        With oDCSLang
            .AddItem(TOP_TITLE, "Data", "Données")
            '  .AddItem(TOP_DESC, "A short DCS dataset description goes here", "[fr] A short DCS dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "Projected changes in statistically downscaled mean temperature (°C) are with respect to the reference period of 1986-2005.",
                     "Les changements projetés dans la température moyenne (°C) statistiquement mise à l'échelle sont calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Mean temperature", "Température moyenne", k)

            k = "tmin"
            .AddItem(VAR_DESC, "Projected changes in statistically downscaled minimum temperature (°C) are with respect to the reference period of 1986-2005.",
                     "Les changements projetés dans la température minimale (°C) statistiquement mise à l'échelle sont calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Daily minimum temperature", "Température minimale quotidienne", k)

            k = "tmax"
            .AddItem(VAR_DESC, "Projected changes in statistically downscaled maximum temperature (°C) are with respect to the reference period of 1986-2005.",
                     "Les changements projetés dans la température maximale (°C) statistiquement mise à l'échelle sont calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Daily maximum temperature", "Température maximale quotidienne", k)

            k = "prec"
            .AddItem(VAR_DESC, "Projected relative changes in statistically downscaled total precipitation are with respect to the reference period of 1986-2005 and expressed as percentage change (%).",
                     "Les changements projetés dans les précipitations totales statistiquement mises à l'échelle sont exprimés en pourcentage (%) et calculés par rapport à la période de référence 1986-2005.", k)
            .AddItem(LAYER_NAME, "Total precipitation", "Précipitations totales", k)

        End With

    End Sub

    Private Function MakeDCSDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"},
            {"PROD", GEOMET_CLIMATE_WMS & "?SERVICE=WMS&VERSION=1.3.0"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeDCSRampId(variable As String, season As String, rcp As String, year As String, lang As String) As String

        Return "DCS_" & variable & "_" & season & "_" & rcp & "_" & year & "_" & lang
    End Function

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


        Dim url As String = MakeDCSDataUrl()

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "TM"}, {"tmin", "TN"}, {"tmax", "TX"}, {"prec", "PR"}}
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
        Dim rampID As String = MakeDCSRampId(variable, season, rcp, year, lang)

        Return MakeWMSLayerConfig(url, rampID, 0.85, False, wmsCode, oDCSLang.Txt(lang, LAYER_NAME, variable), "application/json", template, parser, True)

    End Function

    Private Function MakeDCSLegend(variable As String, season As String, rcp As String, lang As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = MakeDCSDataUrl()

        'precip has different legend than temperature ones
        If variable = "prec" Then
            sLegendUrl &= "&request=GetLegendGraphic&sld_version=1.1.0&layer=DCS.PR.RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"
        Else
            sLegendUrl &= "&request=GetLegendGraphic&sld_version=1.1.0&layer=DCS.TX.RCP85.FALL.2021-2040_PCTL50&format=image/png&STYLE=default"
        End If

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

        With oDCSLang

            'first collate the layers over the years
            Dim lset As String = ""

            For Each year As String In aYear
                Dim rampId As String = MakeDCSRampId(variable, season, rcp, year, lang)
                lset &= MakeLayerLegendBlockConfig("", rampId, "", sCoverIcon, sLegendUrl, "", 4, year <> "2081")

            Next

            'note using variable description as dataset description
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, VAR_DESC, variable)) &
            MakeHiddenExclusiveLegendBlockConfig(lset, 2) &
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
                WriteConfig("ahccd\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeAHCCDLang()
        Dim k As String 'lazy

        oAHCCDLang = New LangHive("AHCCD", oLangParty)

        With oAHCCDLang
            .AddItem(TOP_TITLE, "Data", "Données")
            .AddItem(TOP_DESC, "Adjusted and homogenized station data incorporate adjustments to the original station data to account for discontinuities from non-climatic factors.",
                     "Les données pour les stations climatiques ont été ajustées et homogénéisées pour tenir compte des discontinuités attribuables à des facteurs non climatiques.")

            k = "tmean"
            ' .AddItem(VAR_DESC, "A short homogenized mean temperature description goes here", "[fr] A short mean temperature description goes here", k)
            .AddItem(LAYER_NAME, "Mean temperature", "Température moyenne", k)

            k = "tmin"
            ' .AddItem(VAR_DESC, "A short homogenized minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Daily minimum temperature", "Température minimale quotidienne", k)

            k = "tmax"
            ' .AddItem(VAR_DESC, "A short homogenized maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            .AddItem(LAYER_NAME, "Daily maximum temperature", "Température maximale quotidienne", k)

            k = "prec"
            ' .AddItem(VAR_DESC, "A short adjusted total precipitation description goes here", "[fr] A short precipitation description goes here", k)
            .AddItem(LAYER_NAME, "Total precipitation", "Précipitations totales", k)

            k = "supr"
            ' .AddItem(VAR_DESC, "A short homogenized station pressure description goes here", "[fr] A short surface pressure description goes here", k)
            .AddItem(LAYER_NAME, "Station pressure", "Pression à la station", k)

            k = "slpr"
            ' .AddItem(VAR_DESC, "A short homogenized sea level pressure description goes here", "[fr] A short sea level pressure description goes here", k)
            .AddItem(LAYER_NAME, "Sea level pressure", "Pression au niveau de la mer", k)

            k = "wind"
            ' .AddItem(VAR_DESC, "A short wind speed description goes here", "[fr] A short wind speed description goes here", k)
            .AddItem(LAYER_NAME, "Wind speed", "Vitesse du vent", k)

            'k = "red"
            '.AddItem(LEGEND_TEXT, "Red Circle", "[fr] Red Circle", k)

            'k = "green"
            '.AddItem(LEGEND_TEXT, "Green Circle", "[fr] Green Circle", k)

            'k = "blue"
            '.AddItem(LEGEND_TEXT, "Blue Circle", "[fr] Blue Circle", k)

            'general non-colour one
            .AddItem(LEGEND_TEXT, "AHCCD Station", "Station DCCAH")

        End With

    End Sub

    Private Function MakeAHCCDDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/"},
            {"PROD", GEOMET_WFS & "/collections/ahccd-trends/"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeAHCCDDataLayer(variable As String, season As String, lang As String, rampId As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=temp_mean&period=\"Ann\"

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "temp_mean"}, {"tmin", "temp_min"}, {"tmax", "temp_max"}, {"prec", "total_precip"}, {"supr", "pressure_station"}, {"slpr", "pressure_sea_level"}, {"wind", "wind_speed"}}

        'temp based ones are red. water based is blue. air based is green
        Dim dColour As New Dictionary(Of String, String) From {{"tmean", "#f04116"}, {"tmin", "#f04116"}, {"tmax", "#f04116"}, {"prec", "#0cb8f0"}, {"supr", "#0cf03a"}, {"slpr", "#0cf03a"}, {"wind", "#0cf03a"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "Ann"}, {"MAM", "Spr"}, {"JJA", "Smr"}, {"SON", "Fal"}, {"DJF", "Win"}, {"JAN", "Jan"}, {"FEB", "Feb"}, {"MAR", "Mar"}, {"APR", "Apr"}, {"MAY", "May"}, {"JUN", "Jun"}, {"JUL", "Jul"}, {"AUG", "Aug"}, {"SEP", "Sep"}, {"OCT", "Oct"}, {"NOV", "Nov"}, {"DEC", "Dec"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim colourCode As String = dColour.Item(variable)
        Dim template As String = "assets/templates/ahccd/variables-template.html"
        Dim parser As String = "assets/templates/ahccd/variables-script.js"

        Dim url As String = MakeAHCCDDataUrl() & "items?measurement_type__type_mesure=" & varCode & "&period__periode=" & seasonCode

        Return MakeWFSLayerConfig(url, rampId, 1, True, "station_name__nom_station", oAHCCDLang.Txt(lang, LAYER_NAME, variable), colourCode, template, parser)

    End Function

    Private Function MakeAHCCDLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""


        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}, {"supr", "stnpress"}, {"slpr", "seapress"}, {"wind", "sfcwind"}}
        Dim dLegend As New Dictionary(Of String, String) From {{"tmean", "red"}, {"tmin", "red"}, {"tmax", "red"}, {"prec", "blue"}, {"supr", "green"}, {"slpr", "green"}, {"wind", "green"}}

        Dim sColour As String = dLegend.Item(variable)
        Dim sLegendUrl = "assets/images/" & sColour & "-circle.png"

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

        With oAHCCDLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, "", sCoverIcon, sLegendUrl, .Txt(lang, LEGEND_TEXT), 2,, "icons") &
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

            For Each hour As String In aHour

                Dim nugget As New LangNugget
                For Each lang As String In aLang

                    Dim rampId As String = MakeCAPARampID(var, hour, lang)

                    Dim dataLayers = MakeCAPADataLayer(var, hour, lang, rampId)
                    Dim legund = MakeCAPALegend(var, hour, lang, rampId)
                    Dim support = MakeSupportSet(lang, True, True, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("capa\config-" & FileVar(var) & hour & ".json", fileguts)

            Next

        Next
    End Sub

    Private Function MakeCAPADataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0"},
            {"PROD", GEOMET_WMS & "?SERVICE=WMS&VERSION=1.3.0"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeCAPARampID(variable As String, hour As String, lang As String) As String
        ' doing this due to the radio configuration of the legend.
        ' need to have set of ramp ids, not a singular
        Return "CAPA_" & variable & "_" & hour & "_" & lang
    End Function


    Private Sub MakeCAPALang()
        Dim k As String 'lazy

        oCAPALang = New LangHive("CAPA", oLangParty)

        With oCAPALang
            .AddItem(TOP_TITLE, "Data", "Données")
            .AddItem(TOP_DESC, "The regional deterministic precipitation analysis (RDPA) produces a best estimate of precipitation amounts that occurred over a period of 6 and 24 hours.",
                     "L'Analyse régionale déterministe de précipitations (ARPD) produit une estimation optimale de la quantité de précipitations qui est survenue au cours de périodes passées récentes de 6h ou 24h.")

            'k = "qp25"
            '.AddItem(VAR_DESC, "A short Quantity of Precipitation, 2.5KM resolution description goes here",
            '         "[fr] A short Quantity of Precipitation, 2.5KM resolution description goes here", k)
            '.AddItem(LAYER_NAME, "Quantity of Precipitation, 2.5KM resolution", "[fr] Quantity of Precipitation, 2.5KM resolution", k)

            'note weird trickery here, we are mashing the resolution with the hour period in our key

            k = "qp106"
            '.AddItem(VAR_DESC, "A short Quantity of Precipitation, 10KM resolution description goes here",
            '         "[fr] A short Quantity of Precipitation, 10KM resolution description goes here", k)
            .AddItem(LAYER_NAME, "6 hour precipitation", "Précipitations sur 6 heures", k)

            k = "qp1024"
            .AddItem(LAYER_NAME, "24 hour precipitation", "Précipitations sur 24 heures", k)

            ' .AddItem("CAPA_SLIDER", "Canadian Precipitation Analysis", "[fr] Canadian Precipitation Analysis")

            'k = "24"
            '.AddItem("CAPA_HOUR_DESC", "A short 24 hour description goes here",
            '         "[fr] A short 24 hour description goes here", k)

            'k = "6"
            '.AddItem("CAPA_HOUR_DESC", "A short 6 hour description goes here",
            '         "[fr] A short 6 hour description goes here", k)

        End With

    End Sub

    '''' <summary>
    '''' Makes the Data Layer array for CAPA "set of two time periods"
    '''' </summary>
    '''' <param name="variable"></param>
    '''' <param name="lang"></param>
    '''' <returns></returns>
    'Private Function MakeCAPAHourSet(variable As String, lang As String) As String

    '    'obsolete. they got rid of this fancyness

    '    Dim lset As String = ""

    '    For Each hour As String In aHour
    '        lset = lset & MakeCAPADataLayer(variable, hour, lang) & IIf(hour <> "6", "," & vbCrLf, "")
    '    Next

    '    Return lset

    'End Function

    Private Function MakeCAPADataLayer(variable As String, hour As String, lang As String, rampId As String) As String

        'calculate url (might be a constant)
        'http://geo.weather.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en&LAYERS=HRDPA.6F     

        Dim url As String = MakeCAPADataUrl()

        'TODO make global to prevent re-creating every iteration?
        'might need to add _PR to the key
        Dim dVari As New Dictionary(Of String, String) From {{"qp25", "HRDPA"}, {"qp10", "RDPA"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim wmsCode As String = varCode & "." & hour & "F_PR"

        'derive unique layer id (ramp id)

        Dim template As String = "assets/templates/capa/variables-template.html"
        Dim parser As String = "assets/templates/capa/variables-script.js"

        Return MakeWMSLayerConfig(url, rampId, 0.85, True, wmsCode, oCAPALang.Txt(lang, LAYER_NAME, variable & hour), "text/plain", template, parser, True)

    End Function

    Private Function MakeCAPALegend(variable As String, hour As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        sLegendUrl = MakeCAPADataUrl() & "&request=GetLegendGraphic&sld_version=1.1.0&layer=RDPA.6F_PR&format=image/png&STYLE=default"

        Dim dIcon As New Dictionary(Of String, String) From {{"6", "06h"}, {"24", "24h"}}

        Dim sCoverIcon As String = "assets/images/" & dIcon.Item(hour) & ".png"

        With oCAPALang

            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, "", sCoverIcon, sLegendUrl, "", 2,, "images") &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

    End Function

    ' fancy radio version

    'Private Function MakeCAPALegend(variable As String, lang As String) As String

    '    Dim sLegend As String = ""
    '    Dim sLegendUrl As String = ""

    '    sLegendUrl = "http://geo.weather.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=HRDPA.6F_PR&format=image/png&STYLE=default"

    '    Dim dIcon As New Dictionary(Of String, String) From {{"6", "06h"}, {"24", "24h"}}

    '    Dim season As String

    '    'this can be cleaned up a lot after demo panic

    '    'special logic for exclusive visibility
    '    Dim oConNugget As New ConfigNugget(2)
    '    With oConNugget
    '        .AddLine("{")
    '        .AddLine("""name"": """ & oCAPALang.Txt(lang, "CAPA_SLIDER") & """,", 1)
    '        .AddLine("""expanded"": true,", 1)
    '        .AddLine("""controls"": [""visibility""],", 1)
    '        .AddLine("""children"": [{", 1)
    '        .AddLine("""exclusiveVisibility"": [", 2)

    '        season = "6"
    '        .AddRaw(MakeLayerLegendBlockConfig(
    '                "",
    '                MakeCAPARampID(variable, season, lang),
    '                oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
    '                "assets/images/" & dIcon.Item(season) & ".svg",
    '                sLegendUrl, "", 5))


    '        season = "24"
    '        .AddRaw(MakeLayerLegendBlockConfig(
    '                "",
    '                MakeCAPARampID(variable, season, lang),
    '                oCAPALang.Txt(lang, "CAPA_HOUR_DESC", season),
    '                "assets/images/" & dIcon.Item(season) & ".svg",
    '                sLegendUrl, "", 5, False))

    '        .AddLine("]", 2)
    '        .AddLine("}]", 1)
    '        .AddLine("},")
    '    End With

    '    With oCAPALang

    '        sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
    '        oConNugget.Nugget &
    '        MakeLegendSettingsConfig(lang, True, True, True)
    '    End With

    '    Return sLegend

    'End Function

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
        WriteConfig("hydro\config-hydro.json", fileguts)
    End Sub

    Private Sub MakeHydroLang()

        oHydroLang = New LangHive("HYDRO", oLangParty)

        'we only have one layer. so VAR_DESC might be redundant?

        With oHydroLang
            .AddItem(TOP_TITLE, "Data", "Données")
            .AddItem(TOP_DESC, "This map provides the location of over 1800 hydrometric (water quantity) stations on rivers, streams, and lakes across Canada.",
                     "Cette carte indique l'emplacement de plus de 1800 stations hydrométriques (mesure de quantité d'eau) sur des cours d'eau et des lacs partout au Canada.")

            ' .AddItem(VAR_DESC, "A short hydro description goes here (maybe)", "[fr] A short hydro description goes here (maybe)")
            .AddItem(LAYER_NAME, "Hydrometric stations", "Stations hydrométriques")

            .AddItem(LEGEND_TEXT, "Hydrometric Station", "Stations hydrométriques")

        End With

    End Sub

    Private Function MakeHydroDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/"},
            {"PROD", GEOMET_WFS & "/collections/hydrometric-stations/"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeHydroDataLayer(lang As String, rampID As String) As String

        'calculate url (might be a constant)
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=\%22Active\%22


        Dim url As String = MakeHydroDataUrl() & "items?STATUS_EN=Active"

        Dim template As String = "assets/templates/hydro/stations-template.html"
        Dim parser As String = "assets/templates/hydro/stations-script.js"

        Return MakeWFSLayerConfig(url, rampID, 1, True, "STATION_NAME", oHydroLang.Txt(lang, LAYER_NAME), "#0cb8f0", template, parser)

    End Function

    Private Function MakeHydroLegend(lang As String, rampID As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl = "assets/images/blue-circle.png"

        'TODO need a proper image
        Dim sCoverIcon = "assets/images/blue-circle.png"

        With oHydroLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampID, "", sCoverIcon, sLegendUrl, .Txt(lang, LEGEND_TEXT), 2,, "icons") &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " CanGRID "

    ' WMS. No Time.

    ' NOTE: while there are references to tmin and tmax, we are currently not using them.

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
                WriteConfig("CanGRD\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeCanGRIDLang()
        Dim k As String 'lazy

        oCanGRIDLang = New LangHive("CANGRID", oLangParty)

        With oCanGRIDLang
            .AddItem(TOP_TITLE, "Data", "Données")
            '  .AddItem(TOP_DESC, "A short CanGRID dataset description goes here", "[fr] A short CanGRID dataset description goes here")

            k = "tmean"
            .AddItem(VAR_DESC, "Trend of mean temperature change (°C)",
                     "Tendance des changements de la température moyenne (°C)", k)
            .AddItem(LAYER_NAME, "Mean temperature", "Température moyenne", k)

            k = "prec"
            .AddItem(VAR_DESC, "Trend of relative total precipitation change (%)",
                     "Tendance des changements relatifs des précipitations totales (%)", k)
            .AddItem(LAYER_NAME, "Total precipitation", "Précipitations totales", k)

            'k = "tmin"
            '.AddItem(VAR_DESC, "A short minimum temperature description goes here", "[fr] A short minimum temperature description goes here", k)
            '.AddItem(LAYER_NAME, "Minimum temperature", "[fr] Minimum temperature", k)

            'k = "tmax"
            '.AddItem(VAR_DESC, "A short maximum temperature description goes here", "[fr] A short maximum temperature description goes here", k)
            '.AddItem(LAYER_NAME, "Maximum temperature", "[fr] Maximum temperature", k)

        End With

    End Sub

    Private Function MakCanGRIDDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geomet2-nightly.cmc.ec.gc.ca/geomet-climate?SERVICE=WMS&VERSION=1.3.0"},
            {"PROD", GEOMET_CLIMATE_WMS & "?SERVICE=WMS&VERSION=1.3.0"}}

        Return dUrl.Item(ENV)

    End Function

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

        Dim url As String = MakCanGRIDDataUrl()
        Dim wmsCode As String = "CANGRD.TREND." & varCode & "_" & seasonCode
        Dim template As String = "assets/templates/cangrd/variables-template.html"
        Dim parser As String = "assets/templates/cangrd/variables-script.js"

        Return MakeWMSLayerConfig(url, rampID, 0.85, True, wmsCode, oCanGRIDLang.Txt(lang, LAYER_NAME, variable), "application/json", template, parser, True)

    End Function

    Private Function MakeCanGRIDLegend(variable As String, season As String, lang As String, rampid As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = MakCanGRIDDataUrl()

        'precip has different legend than temperature ones
        If variable = "prec" Then
            sLegendUrl &= "&request=GetLegendGraphic&sld_version=1.1.0&layer=CANGRD.TREND.PR_ANNUAL&format=image/png&STYLE=default"
        Else
            sLegendUrl &= "&request=GetLegendGraphic&sld_version=1.1.0&layer=CANGRD.TREND.TM_ANNUAL&format=image/png&STYLE=default"
        End If

        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

        With oCanGRIDLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, VAR_DESC, variable)) &
            MakeLayerLegendBlockConfig("", rampid, "", sCoverIcon, sLegendUrl, "", 2) &
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
            {"wtpr", "MEM.ETA_WTMP.10"}, {"gh5m", "HIND.MEM.PRES_GZ.500.10"}, {"ta8m", "HIND.MEM.PRES_TT.850.10"}, {"wd2m", "MEM.PRES_UU.200.10"},
            {"wd8m", "HIND.MEM.PRES_UU.850.10"}}

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
            WriteConfig("cansips\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeCanSIPSLang()
        Dim k As String 'lazy

        oCanSIPSLang = New LangHive("CANSIPS", oLangParty)

        With oCanSIPSLang
            .AddItem(TOP_TITLE, "Data", "Données")
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

    Private Function MakeCanSIPSDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geomet2-nightly.cmc.ec.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0"},
            {"PROD", GEOMET_WMS & "?SERVICE=WMS&VERSION=1.3.0"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeCanSIPSDataLayer(variable As String, lang As String, rampID As String, wmsID As String) As String

        'calculate url (might be a constant)
        'tmean , tmin , tmax , prec , surface pres , sea pres , whind
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities&lang=en

        Dim url As String = MakeCanSIPSDataUrl()
        Dim template As String = "assets/templates/cansips/variables-template.html"
        Dim parser As String = "assets/templates/cansips/variables-script.js"

        Return MakeWMSLayerConfig(url, rampID, 0.85, True, wmsID, oCanSIPSLang.Txt(lang, LAYER_NAME, variable), "text/plain", template, parser, True)

    End Function

    Private Function MakeCanSIPSLegend(variable As String, lang As String, rampid As String, wmsID As String) As String

        Dim sLegend As String = ""
        Dim sLegendUrl As String = ""

        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANSIPS.HIND.MEM.ETA_PN-SLP.10&format=image/png&STYLE=default
        'http://geomet2-nightly.cmc.ec.gc.ca/geomet?version=1.3.0&service=WMS&request=GetLegendGraphic&sld_version=1.1.0&layer=CANSIPS.MEM.ETA_RT.10&format=image/png&STYLE=default

        'precip has different legend than temperature ones

        sLegendUrl = MakeCanSIPSDataUrl() & "&request=GetLegendGraphic&sld_version=1.1.0&layer=" & wmsID & "&format=image/png&STYLE=default"

        'TODO need proper icons
        Dim dIcon As New Dictionary(Of String, String) From {{"slpr", "HIND.MEM.ETA_PN-SLP.10"}, {"itpr", "MEM.ETA_RT.10"}, {"stpr", "MEM.ETA_TT.10"},
            {"wtpr", "MEM.ETA_WTMP.10"}, {"gh5m", "PRES_GZ.500.10"}, {"ta8m", "PRES_TT.850.10"}, {"wd2m", "MEM.PRES_UU.200.10"},
            {"wd8m", "PRES_UU.850.10"}}

        Dim sCoverIcon = "assets/images/" & FileVar(variable) & ".png"

        With oCanSIPSLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampid, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, True, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Daily "

    'this block is obsolete / out of date / we've just been using a static file in the main app

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
            WriteConfig("daily\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeDailyLang()
        Dim k As String 'lazy

        oDailyLang = New LangHive("DAILY", oLangParty)

        With oDailyLang
            .AddItem(TOP_TITLE, "Data", "Données")
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

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

        With oDailyLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2) &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function

#End Region

#Region " Monthly "

    'this block is obsolete / out of date / we've just been using a static file in the main app

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
            WriteConfig("monthly\config-" & FileVar(var) & ".json", fileguts)

        Next
    End Sub

    Private Sub MakeMonthlyLang()
        Dim k As String 'lazy

        oMonthlyLang = New LangHive("MONTHLY", oLangParty)

        With oMonthlyLang
            .AddItem(TOP_TITLE, "Data", "Données")
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

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

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
                WriteConfig("normal\config-" & FileVar(var) & "-" & FileSeason(season) & ".json", fileguts)
            Next
        Next
    End Sub

    Private Sub MakeNormalsLang()
        Dim k As String 'lazy

        oNormalsLang = New LangHive("NORMALS", oLangParty)

        With oNormalsLang
            .AddItem(TOP_TITLE, "Data", "Données")
            .AddItem(TOP_DESC, "Climate Normals and Averages are used to summarize or describe the average climatic conditions of a particular location.\n\nAt the completion of each decade, Environment and Climate Change Canada updates its Climate Normals for as many locations and as many climatic characteristics as possible. The Climate Normals, Averages and Extremes offered here are based on Canadian climate stations with at least 15 years of data between 1981 to 2010.",
                     "Les normales et moyennes climatiques sont utilisées pour résumer ou décrire les conditions climatiques moyennes d'un endroit donné.\n\nÀ la fin de chaque décennie, Environnement et Changement climatique Canada met à jour ses normales climatiques pour autant d'endroits et autant de caractéristiques climatiques que possible. Les  normales, moyennes et extrêmes climatiques présentées ici sont fondées sur les données des stations climatologiques canadiennes ayant au moins 15 années de données entre 1981 et 2010.")

            k = "tmean"
            .AddItem(VAR_DESC, "The mean temperature in degrees Celsius (°C) is defined as the average of the maximum and minimum temperature at a location for a specified time interval.",
                     "La température moyenne en degrés Celsius (°C) est définie comme la moyenne des températures maximale et minimale à un endroit durant une période précise.", k)
            .AddItem(LAYER_NAME, "Mean temperature", "Température moyenne", k)

            k = "tmin"
            .AddItem(VAR_DESC, "The average of the minimum temperature in degrees Celsius (°C) observed at the location for that month.",
                     "La moyenne des températures minimales en degrés Celsius (°C) observées à un endroit durant ce mois.", k)
            .AddItem(LAYER_NAME, "Daily minimum temperature", "Température minimale quotidienne", k)

            k = "tmax"
            .AddItem(VAR_DESC, "The average of the maximum temperature in degrees Celsius (°C) observed at the location for that month.",
                     "La moyenne des températures maximales en degrés Celsius (°C) observées à un endroit durant ce mois.", k)
            .AddItem(LAYER_NAME, "Daily maximum temperature", "Température maximale quotidienne", k)

            k = "prec"
            .AddItem(VAR_DESC, "The sum of the total rainfall and the water equivalent of the total snowfall in millimetres (mm), observed at the location during a specified time interval.",
                     "La somme de la quantité totale de pluie et de l'équivalent en eau des chutes de neige totales, en millimètres (mm), observés à un endroit durant une période précise.", k)
            .AddItem(LAYER_NAME, "Total precipitation", "Précipitations totales", k)

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

            'grid columns
            .AddItem(COLUMN_NAME, "x", "x", "OBJECTID")
            .AddItem(COLUMN_NAME, "y", "y", "rvInternalCoordX")
            .AddItem(COLUMN_NAME, "z", "z", "rvInternalCoordY")
            .AddItem(COLUMN_NAME, "Normal code", "Code de la normale", "NORMAL_CODE")
            .AddItem(COLUMN_NAME, "a", "a", "OCCURRENCE_COUNT")
            .AddItem(COLUMN_NAME, "b", "b", "FIRST_YEAR")
            .AddItem(COLUMN_NAME, "Period beginning", "Début de la période", "PERIOD_BEGIN")
            .AddItem(COLUMN_NAME, "Percentage of possible observations", "Pourcentage d'observations possibles", "PERCENT_OF_POSSIBLE_OBS")
            .AddItem(COLUMN_NAME, "Period end", "Fin de la période", "PERIOD_END")
            .AddItem(COLUMN_NAME, "Last year of normal period", "Dernière année de la période pour les normales", "LAST_YEAR_NORMAL_PERIOD")
            .AddItem(COLUMN_NAME, "Province / Territory", "Province / Territoire", "PROVINCE_CODE")
            .AddItem(COLUMN_NAME, "f", "f", "PUBLICATION_CODE")
            .AddItem(COLUMN_NAME, "First year of normal period", "Première année de la période pour les normales", "FIRST_YEAR_NORMAL_PERIOD")
            .AddItem(COLUMN_NAME, "h", "h", "MAX_DURATION_MISSING_YEARS")
            .AddItem(COLUMN_NAME, "MSC station name", "Nom de la station du SMC", "STATION_NAME")
            .AddItem(COLUMN_NAME, "i", "i", "CURRENT_FLAG")
            .AddItem(COLUMN_NAME, "j", "j", "LAST_YEAR")
            .AddItem(COLUMN_NAME, "Date calculated", "Date du calcul", "DATE_CALCULATED")
            .AddItem(COLUMN_NAME, "", "Type de mesure", "FRE_PUB_NAME")
            .AddItem(COLUMN_NAME, "Total observations count", "Nombre total d'observations", "TOTAL_OBS_COUNT")
            .AddItem(COLUMN_NAME, "k", "k", "PERIOD")
            .AddItem(COLUMN_NAME, "Climate value", "Valeur climatique", "VALUE")
            .AddItem(COLUMN_NAME, "l", "l", "STN_ID")
            .AddItem(COLUMN_NAME, "m", "m", "NORMAL_ID")
            .AddItem(COLUMN_NAME, "n", "n", "ID")
            .AddItem(COLUMN_NAME, "o", "o", "FIRST_OCCURRENCE_DATE")
            .AddItem(COLUMN_NAME, "Month", "Mois", "MONTH")
            .AddItem(COLUMN_NAME, "q", "q", "YEAR_COUNT_NORMAL_PERIOD")
            .AddItem(COLUMN_NAME, "Measurement type", "", "ENG_PUB_NAME")


        End With

    End Sub

    Private Function MakeNormalsDataUrl() As String
        Dim dUrl As New Dictionary(Of String, String) From {
            {"DEV", "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/climate-normals/"},
            {"PROD", GEOMET_WFS & "/collections/climate-normals/"}}

        Return dUrl.Item(ENV)

    End Function

    Private Function MakeNormalsGridArray(lang As String) As Object
        Dim magicArray = {
            {"OBJECTID", "", False},
            {"rvInternalCoordX", "", False},
            {"rvInternalCoordY", "", False},
            {"NORMAL_CODE", "", True},
            {"OCCURRENCE_COUNT", "", False},
            {"FIRST_YEAR", "", False},
            {"PERIOD_END", "", True},
            {"PERCENT_OF_POSSIBLE_OBS", "", True},
            {"PERIOD_BEGIN", "", True},
            {"LAST_YEAR_NORMAL_PERIOD", "", True},
            {"PROVINCE_CODE", "", True},
            {"PUBLICATION_CODE", "", False},
            {"FIRST_YEAR_NORMAL_PERIOD", "", True},
            {"MAX_DURATION_MISSING_YEARS", "", False},
            {"STATION_NAME", "", True},
            {"CURRENT_FLAG", "", False},
            {"LAST_YEAR", "", False},
            {"DATE_CALCULATED", "", True},
            {"FRE_PUB_NAME", "", lang = "fr"},
            {"TOTAL_OBS_COUNT", "", True},
            {"PERIOD", "", False},
            {"VALUE", "", True},
            {"STN_ID", "", False},
            {"NORMAL_ID", "", False},
            {"ID", "", False},
            {"FIRST_OCCURRENCE_DATE", "", False},
            {"MONTH", "", True},
            {"YEAR_COUNT_NORMAL_PERIOD", "", False},
            {"ENG_PUB_NAME", "", lang = "en"}
        }

        'enhance the language
        For iCol = 0 To magicArray.GetUpperBound(0)
            If magicArray(0, 2) Then
                magicArray(iCol, 1) = oNormalsLang.Txt(lang, COLUMN_NAME, magicArray(iCol, 0))
            End If
        Next


        Return magicArray

    End Function

    Private Function MakeNormalsDataLayer(variable As String, season As String, lang As String, rampId As String) As String

        'TODO layer is currently down.  still need to see how data/service is structured

        'TODO make global to prevent re-creating every iteration?
        Dim dVari As New Dictionary(Of String, String) From {{"tmean", "1"}, {"tmin", "8"}, {"tmax", "5"}, {"prec", "56"}}
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "13"}, {"JAN", "1"}, {"FEB", "2"}, {"MAR", "3"}, {"APR", "4"}, {"MAY", "5"}, {"JUN", "6"}, {"JUL", "7"}, {"AUG", "8"}, {"SEP", "9"}, {"OCT", "10"}, {"NOV", "11"}, {"DEC", "12"}}
        Dim dColour As New Dictionary(Of String, String) From {{"tmean", "#f04116"}, {"tmin", "#f04116"}, {"tmax", "#f04116"}, {"prec", "#0cb8f0"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)
        Dim template As String = "assets/templates/normal/variables-template.html"
        Dim parser As String = "assets/templates/normal/variables-script.js"


        Dim url As String = MakeNormalsDataUrl() & "items?NORMAL_ID=" & varCode & "&MONTH=" & seasonCode

        Return MakeWFSLayerConfig(url, rampId, 1, True, "STN_ID", oNormalsLang.Txt(lang, LAYER_NAME, variable), dColour.Item(variable), template, parser)

    End Function

    Private Function MakeNormalsLegend(variable As String, season As String, lang As String, rampId As String) As String

        Dim sLegend As String = ""

        'TODO update icons
        Dim dIcon As New Dictionary(Of String, String) From {{"tmean", "tmean"}, {"tmin", "tmin"}, {"tmax", "tmax"}, {"prec", "precip"}}
        Dim dLegend As New Dictionary(Of String, String) From {{"tmean", "red"}, {"tmin", "red"}, {"tmax", "red"}, {"prec", "blue"}}

        Dim sColour As String = dLegend.Item(variable)
        Dim sLegendUrl = "assets/images/" & sColour & "-circle.png"

        Dim sCoverIcon = "assets/images/" & dIcon.Item(variable) & ".png"

        With oNormalsLang
            sLegend &= MakeLegendTitleConfig(.Txt(lang, TOP_TITLE), .Txt(lang, TOP_DESC)) &
            MakeLayerLegendBlockConfig("", rampId, .Txt(lang, VAR_DESC, variable), sCoverIcon, sLegendUrl, "", 2,, "icons") &
            MakeLegendSettingsConfig(lang, True, False, True)
        End With

        Return sLegend

    End Function




#End Region


#Region " Fancy Extra Buttons "

    Private Sub cmdCopy_Click(sender As Object, e As EventArgs) Handles cmdCopy.Click

        ENV = cboEnv.Text.Trim

        Dim APP_CONFIGS As New Dictionary(Of String, String) From {
            {"DEV", "C:\Git\CCCS_Viewer\assets\configs\"},
            {"PROD", "C:\Git\CCCS_Viewer\assets\configs-multi\"}}

        'Const VER As String = "1"
        Dim aDatasets = {"ahccd", "cangrd", "capa", "cmip5", "dcs", "hydro", "normal"}

        For Each ds In aDatasets
            Dim sPathNugget As String = ds & "\" ' & VER & "\"
            Dim sSourceDir As String = DUMP_FOLDER & sPathNugget
            Dim sTargetDir As String = APP_CONFIGS.Item(ENV) & sPathNugget

            Dim oSrcDir As New DirectoryInfo(sSourceDir)
            Dim aFiles = oSrcDir.GetFiles()

            For Each oFile In aFiles
                File.Copy(sSourceDir & oFile.Name, sTargetDir & oFile.Name, True)
            Next

        Next

        MsgBox("copied, thanks")

    End Sub

    Private Sub cmdLang_Click(sender As Object, e As EventArgs) Handles cmdLang.Click
        If oLangParty.Count = 0 Then
            MsgBox("Run the main grinder first.")
        Else
            Dim oFile As StreamWriter = New StreamWriter(DUMP_FOLDER & "langdump.txt", False)
            For Each oL In oLangParty
                oFile.WriteLine(oL)
            Next
            oFile.Close()
            MsgBox("Lang dumped done thanks")
        End If
    End Sub


#End Region

End Class
