Imports System.IO

Public Class ConfigForm

    ' arrays of domains. global scope for sharing fun
    Shared aLang = {"en", "fr"}
    Shared aRcp = {"rcp26", "rcp45", "rcp85"}
    Shared aAHCCDVar = {"tmean", "tmin", "tmax", "prec", "supr", "slpr", "wind"}
    Shared aCAPAVar = {"qp25", "qp10"}
    Shared aCMIP5Var = {"snow", "sith", "sico", "wind"}
    Shared aDCSVar = {"tmean", "tmin", "tmax", "prec"}
    Shared aSeason = {"ANN", "MAM", "JJA", "SON", "DJF"}
    Shared aSeasonMonth = {"ANN", "MAM", "JJA", "SON", "DJF", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"}
    Shared aYear = {"2021", "2041", "2061", "2081"}
    Shared aHour = {"24", "6"} 'want this order on time slider


    Const DUMP_FOLDER As String = "c:\git\configotron\configotron\dump\"
    Const PAD1 As String = "    "
    Const PAD2 As String = "      "
    Const PAD3 As String = "        "



    Private Sub cmdEnhanceMini_Click(sender As Object, e As EventArgs) Handles cmdEnhanceMini.Click

        'MAIN STARTING POINT OF APP.

        MakeCMIP5Configs()
        MakeDCSConfigs()
        MakeAHCCDConfigs()
        MakeCAPAConfigs()
        MakeHydroConfigs()

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
    Private Function MakeWMSLayerConfig(url As String, rampId As String, opacity As Double, visible As Boolean, wmsLayerId As String) As String
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

        'TODO if we need the 'name' element, will need another input param
        'TODO do we need to have a STYLE parameter added?
        'TODO most likely remove data parameter, unless we add in json table, then might need it

        Dim json As String = PAD1 & "{" & vbCrLf &
            PAD2 & """id"": """ & rampId & """," & vbCrLf &
            PAD2 & """layerType"":  ""ogcWms""," & vbCrLf &
            PAD2 & """url"": """ & url & """," & vbCrLf &
            PAD2 & """state"": {" & vbCrLf &
            PAD3 & """opacity"": " & opacity & "," & vbCrLf &
            PAD3 & """visibility"": " & BoolToJson(visible) & vbCrLf &
            PAD2 & "}," & vbCrLf &
            PAD2 & """layerEntries"": [{""id"": """ & wmsLayerId & """ }]," & vbCrLf &
            PAD2 & """controls"": [""data""]" & vbCrLf &
            PAD1 & "}"

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

        'TODO if we need the 'name' element, will need another input param
        'TODO verify that we can delete the "control" part for tiles.
        'TODO we might also want "controls": ["visibility", "opacity", "reload", "settings"],
        Dim json As String = PAD1 & "{" & vbCrLf &
            PAD2 & """id"": """ & id & """," & vbCrLf &
            PAD2 & """layerType"": ""esriTile""," & vbCrLf &
            PAD2 & """url"": """ & url & """," & vbCrLf &
            PAD2 & """state"": {" & vbCrLf &
            PAD3 & """opacity"": " & opacity & "," & vbCrLf &
            PAD3 & """visibility"": " & BoolToJson(visible) & vbCrLf &
            PAD2 & "}" & vbCrLf &
            PAD1 & "}"

        ' pad2 & """controls"": [""data""]" & vbCrLf &

        Return json

    End Function

    Private Function MakeWFSLayerConfig(url As String, id As String, opacity As Double, visible As Boolean, nameField As String) As String
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

        'TODO if we need the 'name' element, will need another input param
        Dim json As String = PAD1 & "{" & vbCrLf &
            PAD2 & """id"": """ & id & """," & vbCrLf &
            PAD2 & """layerType"": ""ogcWfs""," & vbCrLf &
            PAD2 & """url"": """ & url & """," & vbCrLf &
            PAD2 & """nameField"": """ & nameField & """," & vbCrLf &
            PAD2 & """state"": {" & vbCrLf &
            PAD3 & """opacity"": " & opacity & "," & vbCrLf &
            PAD3 & """visibility"": " & BoolToJson(visible) & vbCrLf &
            PAD2 & "}," & vbCrLf &
            PAD2 & """controls"": [""data""]" & vbCrLf &
            PAD1 & "}"

        Return json

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
            "}"

        Return json
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
    ''' Writes content to a text file
    ''' </summary>
    ''' <param name="filename"></param>
    ''' <param name="content"></param>
    Private Sub WriteConfig(filename As String, content As String)

        Dim oFile As StreamWriter = New StreamWriter(DUMP_FOLDER & filename, False)
        oFile.Write(content)
        oFile.Close()

    End Sub

#Region " Support Layers "

    Private Function MakeSupportSet(city As Boolean, prov As Boolean, labels As Boolean) As String
        Dim sGuts As String = ""

        If city Then
            sGuts = sGuts & MakeCitiesConfig() & "," & vbCrLf
        End If

        If prov Then
            sGuts = sGuts & MakeProvinceConfig() & "," & vbCrLf
        End If

        If labels Then
            sGuts = sGuts & MakeLabelsConfig() & "," & vbCrLf
        End If

        'trim last comma
        Return sGuts.Substring(0, sGuts.Length - 3)

    End Function

    Private Function MakeProvinceConfig() As String

        Return MakeTileLayerConfig("http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/Overlays/Provinces/MapServer", "prov_support", 1, True)
    End Function

    Private Function MakeCitiesConfig() As String

        Return MakeTileLayerConfig("http://vmarcgisdev01.canadaeast.cloudapp.azure.com/arcgis/rest/services/Overlays/Cities/MapServer", "city_support", 1, False)
    End Function

    Private Function MakeLabelsConfig() As String

        Return MakeTileLayerConfig("http://geoappext.nrcan.gc.ca/arcgis/rest/services/BaseMaps/CBMT_TXT_3978/MapServer", "labels_support", 1, True)
    End Function

#End Region

#Region " CMIP5 "

    Private Sub MakeCMIP5Configs()
        For Each var As String In aCMIP5Var
            For Each season As String In aSeason
                For Each rcp As String In aRcp
                    Dim nugget As New LangNugget
                    For Each lang As String In aLang
                        Dim dataLayers = MakeCMIP5YearSet(var, season, rcp, lang)
                        Dim legund = MakeCMIP5Legend(var, season, rcp, lang)
                        Dim support = MakeSupportSet(True, True, True)

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

        Return MakeWMSLayerConfig("url", "id", 1, True, "wmslayer")

    End Function

    Private Function MakeCMIP5Legend(variable As String, season As String, rcp As String, lang As String) As String

        Return "{ ""legend"": true }"

    End Function

#End Region

#Region " DCS "

    ''' <summary>
    ''' Create set of config files for DCS
    ''' </summary>
    Private Sub MakeDCSConfigs()
        For Each var As String In aDCSVar
            For Each season As String In aSeason
                For Each rcp As String In aRcp
                    Dim nugget As New LangNugget
                    For Each lang As String In aLang
                        Dim dataLayers = MakeDCSYearSet(var, season, rcp, lang)  ' TODO we may need to add a 5th year period for "historical"
                        Dim legund = MakeDCSLegend(var, season, rcp, lang)
                        Dim support = MakeSupportSet(True, True, True)

                        Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                        nugget.setLang(lang, configstruct)
                    Next

                    Dim fileguts = MakeLangStructure(nugget)
                    WriteConfig("testdcs_" & var & season & rcp & ".json", fileguts)

                Next
            Next
        Next
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

        Return MakeWMSLayerConfig(url, rampID, 1, True, wmsCode)

    End Function

    Private Function MakeDCSLegend(variable As String, season As String, rcp As String, lang As String) As String

        Return "{ ""legend"": true }"

    End Function

#End Region

#Region " AHCCD "
    ''' <summary>
    ''' Create set of config files for AHCCD
    ''' </summary>
    Private Sub MakeAHCCDConfigs()
        For Each var As String In aAHCCDVar
            For Each season As String In aSeasonMonth

                'TODO there are no differences in service URL for language.
                '     if we dont need langauge anywhere else, we can simpley do one configstruct then assign to both langs in the nugget
                Dim nugget As New LangNugget
                For Each lang As String In aLang
                    Dim dataLayers = MakeAHCCDDataLayer(var, season, lang)
                    Dim legund = MakeAHCCDLegend(var, season, lang)
                    Dim support = MakeSupportSet(True, True, True)

                    Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                    nugget.setLang(lang, configstruct)
                Next

                Dim fileguts = MakeLangStructure(nugget)
                WriteConfig("testAHCCD_" & var & season & ".json", fileguts)
            Next
        Next
    End Sub


    Private Function MakeAHCCDDataLayer(variable As String, season As String, lang As String) As String
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
        Dim dSeason As New Dictionary(Of String, String) From {{"ANN", "Ann"}, {"MAM", "Spr"}, {"JJA", "Smr"}, {"SON", "Fal"}, {"DJF", "Win"}, {"JAN", "Jan"}, {"FEB", "Feb"}, {"MAR", "Mar"}, {"APR", "Apr"}, {"MAY", "May"}, {"JUN", "Jun"}, {"JUL", "Jul"}, {"AUG", "Aug"}, {"SEP", "Sep"}, {"OCT", "Oct"}, {"NOV", "Nov"}, {"DEC", "Dec"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)
        Dim seasonCode As String = dSeason.Item(season)

        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/ahccd-trends/items?measurement_type=" & varCode & "&period=" & seasonCode


        'derive unique layer id (ramp id)
        Dim rampID As String = "AHCCD_" & variable & "_" & season & "_" & lang

        Return MakeWFSLayerConfig(url, rampID, 1, True, "station_id")

    End Function

    Private Function MakeAHCCDLegend(variable As String, season As String, lang As String) As String

        Return "{ ""legend"": true }"

    End Function

#End Region

#Region " CAPA "

    ''' <summary>
    ''' Create set of config files for CAPA
    ''' </summary>
    Private Sub MakeCAPAConfigs()
        For Each var As String In aCAPAVar


            Dim nugget As New LangNugget
            For Each lang As String In aLang
                Dim dataLayers = MakeCAPAHourSet(var, lang)
                Dim legund = MakeCAPALegend(var, lang)
                Dim support = MakeSupportSet(True, True, True)

                Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

                nugget.setLang(lang, configstruct)
            Next

            Dim fileguts = MakeLangStructure(nugget)
            WriteConfig("testCAPA_" & var & ".json", fileguts)

        Next
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
        Dim dVari As New Dictionary(Of String, String) From {{"qp25", "HRDPA"}, {"qp10", "RDPA"}}

        'calculate wms layer id
        Dim varCode As String = dVari.Item(variable)

        Dim wmsCode As String = varCode & "." & hour & "F"

        'derive unique layer id (ramp id)
        Dim rampID As String = "CAPA_" & variable & "_" & hour & "_" & lang

        Return MakeWMSLayerConfig(url, rampID, 1, True, wmsCode)

    End Function

    Private Function MakeCAPALegend(variable As String, lang As String) As String

        Return "{ ""legend"": true }"

    End Function

#End Region

#Region " Hydrometric "

    ''' <summary>
    ''' Create set of config files for Hydro
    ''' </summary>
    Private Sub MakeHydroConfigs()

        'TODO there are no differences in service URL for language.
        '     if we dont need langauge anywhere else, we can simpley do one configstruct then assign to both langs in the nugget
        Dim nugget As New LangNugget
        For Each lang As String In aLang
            Dim dataLayers = MakeHydroDataLayer(lang)
            Dim legund = MakeHydroLegend(lang)
            Dim support = MakeSupportSet(True, True, True)

            Dim configstruct = MakeConfigStructure(legund, support, dataLayers)

            nugget.setLang(lang, configstruct)
        Next

        Dim fileguts = MakeLangStructure(nugget)
        WriteConfig("testHydro.json", fileguts)
    End Sub


    Private Function MakeHydroDataLayer(lang As String) As String
        'TODO attempt to get a URL that works with &lang but without GetCapabilities.
        '     the get capabilities is 8mb on public geomet.
        '     need aly's CORS patch done before I can test this
        '     Mike suggestion to duplicate the layer id arg on the main url 

        'calculate url (might be a constant)
        'http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=\%22Active\%22


        Dim url As String = "http://geo.wxod-dev.cmc.ec.gc.ca/geomet/features/collections/hydrometric-stations/items?STATUS_EN=Active"

        'derive unique layer id (ramp id)
        Dim rampID As String = "Hydro_" & lang

        Return MakeWFSLayerConfig(url, rampID, 1, True, "STATION_NAME")

    End Function

    Private Function MakeHydroLegend(lang As String) As String

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

#End Region

End Class
