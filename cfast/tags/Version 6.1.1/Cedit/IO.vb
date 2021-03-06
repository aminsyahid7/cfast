Imports System
Imports System.IO
Module IO
    Public Sub ReadThermalProperties(ByVal FileName As String)
        'Simple read of thermal properties file. There is no checking of validity. 
        Dim csv As New CSVsheet(FileName)
        Dim i, j As Integer
        Dim hcl(6) As Single
        If csv.MaxRow > 0 Then myThermalProperties.Clear() 'Only clear old stuff if new file exists
        For i = 1 To csv.MaxRow
            If Not SkipLine(csv.str(i, CFASTlnNum.keyWord)) Then
                myThermalProperties.Add(New ThermalProperty(csv.str(i, thermalNum.shortName), _
                    csv.str(i, thermalNum.longName), csv.Num(i, thermalNum.Conductivity), _
                    csv.Num(i, thermalNum.specificHeat), csv.Num(i, thermalNum.density), csv.Num(i, thermalNum.thickness), _
                    csv.Num(i, thermalNum.emissivity)))
                For j = 0 To 6
                    hcl(j) = csv.Num(i, thermalNum.HClCoefficients + j)
                Next
                myThermalProperties.Item(myThermalProperties.Count - 1).SetHCl(hcl)
            ElseIf Not DropComment(csv.str(i, CFASTlnNum.keyWord)) Then
                thermalFileComments.Add(csv.strrow(i))
            End If
            If myThermalProperties.Count > 0 Then _
                myThermalProperties.Item(myThermalProperties.Count - 1).Changed = False
        Next
        myThermalProperties.FileName = FileName
        myThermalProperties.FileChanged = False
    End Sub
    Public Sub ReadFireObjects(ByVal pathName As String)
        ' Simple routine that gets all *.o files in PathName and opens them
        Dim dir As String() = Directory.GetFiles(pathName, "*.o")
        Dim file As String
        For Each file In dir
            ReadFireObject(file)
        Next
    End Sub
    Public Sub ReadFireObject(ByVal filenm As String)
        'simple read of a fire file, no checking and comments are ignored
        Dim csv As New CSVsheet(filenm)
        Dim fireComments As New Collection
        Dim i, j As Integer
        Dim rowidx(csv.MaxRow) As Integer
        Dim rdx As Integer = 0
        For i = 1 To csv.MaxRow
            If Not SkipLine(csv.CSVrow(i)) Then
                rowidx(rdx) = i
                rdx += 1
            ElseIf Comment(csv.str(i, CFASTlnNum.keyWord)) Then
                fireComments.Add(csv.strrow(i))
            End If
        Next
        For i = 0 To myFireObjects.Count - 1
            If csv.str(rowidx(0), 1) = myFireObjects.Item(i).Name Then
                Exit Sub
            End If
        Next
        myFireObjects.Add(New Fire(csv.str(rowidx(0), 1), csv.Num(rowidx(8), 1), csv.Num(rowidx(9), 1), _
         csv.Num(rowidx(10), 1), csv.Num(rowidx(2), 1), csv.Num(rowidx(7), 1), csv.Num(rowidx(3), 1), _
         csv.Num(rowidx(4), 1), csv.Num(rowidx(11), 1), csv.Num(rowidx(5), 1), csv.Num(rowidx(6), 1)))
        myFireObjects(myFireObjects.Count - 1).Material = csv.str(rowidx(12), 1)
        Dim firedata(12, CInt(csv.Num(rowidx(1), 1) - 1)) As Single
        For i = 0 To csv.Num(rowidx(1), 1) - 1
            For j = 0 To 12
                firedata(j, i) = csv.Num(rowidx(1 + i), firefile(j))
            Next
        Next
        myFireObjects(myFireObjects.Count - 1).SetFireData(firedata)
        fireFilesComments.Add(fireComments)
        myFireObjects(myFireObjects.Count - 1).CommentsIndex = fireFilesComments.Count
        myFireObjects(myFireObjects.Count - 1).Changed = False
    End Sub
    Public Sub WriteFireObjects(ByVal pathName As String)
        If myFireObjects.Changed Then
            Dim i As Integer
            Dim writeFile As String

            For i = 0 To myFireObjects.Count - 1
                If myFireObjects(i).Changed Then
                    writeFile = pathName + myFireObjects(i).Name.Trim + ".o"
                    WriteFireObject(i, writeFile)
                    myFireObjects(i).Changed = False
                End If
            Next
        End If
    End Sub
    Public Sub WriteFireObject(ByVal index As Integer, ByVal FileName As String)
        ' Write fire object specified by the index
        Dim aFire As Fire
        Dim csv As New CSVsheet

        aFire = myFireObjects(index)
        csv.str(1, 1) = aFire.Name
        csv.Num(3, 1) = aFire.MolarMass
        csv.Num(4, 1) = aFire.VolitilTemp + 100.0
        csv.Num(5, 1) = aFire.VolitilTemp
        csv.Num(6, 1) = aFire.HeatofGasification
        csv.Num(7, 1) = aFire.RadiativeFraction
        csv.Num(8, 1) = aFire.TotalMass
        csv.Num(9, 1) = aFire.Length
        csv.Num(10, 1) = aFire.Width
        csv.Num(11, 1) = aFire.Thickness
        csv.Num(12, 1) = aFire.HeatofCombustion
        csv.str(13, 1) = aFire.Material

        Dim firedata(12, 0) As Single, i, j As Integer
        aFire.GetFireData(firedata, csv.Num(2, 1))
        For i = 0 To csv.Num(2, 1)
            For j = 0 To 12
                csv.Num(i + 2, firefile(j)) = firedata(j, i)
            Next
        Next
        csv.Num(2, 1) += 1
        Dim fireComments As New Collection
        Dim offset As Integer = Math.Max(13, 1 + csv.Num(2, 1))
        If aFire.CommentsIndex > 0 Then
            fireComments = fireFilesComments.Item(aFire.CommentsIndex)
            For j = 1 To fireComments.Count
                csv.str(offset + j, 1) = fireComments.Item(j)
            Next
        End If
        csv.WrtCSVfile(FileName)
    End Sub
    Public Sub WriteThermalProperties(ByVal FileName As String)
        If myThermalProperties.Changed Then
            Dim csv As New CSVsheet
            Dim aThermal As ThermalProperty
            Dim i As Integer = 1
            Dim j, k As Integer
            Dim hcl() As Single
            AddThermalHeader(csv, i)
            For j = 0 To myThermalProperties.Count - 1
                aThermal = myThermalProperties.Item(j)
                aThermal.GetThermalProperties(csv.str(i, thermalNum.shortName), csv.str(i, thermalNum.longName), _
                    csv.Num(i, thermalNum.Conductivity), csv.Num(i, thermalNum.specificHeat), csv.Num(i, thermalNum.density), _
                    csv.Num(i, thermalNum.thickness), csv.Num(i, thermalNum.emissivity))
                aThermal.GetHCl(hcl)
                For k = 0 To 6
                    csv.Num(i, thermalNum.HClCoefficients + k) = hcl(k)
                Next
                i += 1
            Next
            For j = 1 To thermalFileComments.Count
                csv.CSVrow(i) = thermalFileComments.Item(j)
                i += 1
            Next
            csv.WrtCSVfile(FileName)
        End If
    End Sub
    Public Sub AddThermalHeader(ByRef csv As CSVsheet, ByRef i As Integer)
        Dim j As Integer = 0
        csv.str(i, CFASTlnNum.keyWord + j) = "!!Short Name"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "Conductivity"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "Specific Heat"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "Density"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "Thickness"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "Emissivity"
        j += 1
        csv.str(i, CFASTlnNum.keyWord + j) = "HCl Coefficients"
        j += 7
        csv.str(i, CFASTlnNum.keyWord + j) = "Long Name"
        i += 1
    End Sub
    Public Sub WriteDataFile(ByVal FileName As String)
        Dim csv As New CSVsheet
        Dim i As Integer = 1
        Dim j As Integer

        ' write header line
        csv.str(i, CFASTlnNum.keyWord) = "VERSN"
        csv.Num(i, CFASTlnNum.version) = 6
        csv.str(i, CFASTlnNum.title) = myEnvironment.Title
        i += 1
        For j = 1 To dataFileHeader.Count
            csv.CSVrow(i) = """" + dataFileHeader.Item(j) + """"
            i += 1
        Next
        'comment for environmental section
        AddHeadertoOutput(csv, i, "Environmental Keywords")
        'Time line
        csv.str(i, CFASTlnNum.keyWord) = "TIMES"
        csv.Num(i, timesNum.simTime) = myEnvironment.SimulationTime
        If DetailedCFASTOutput Then
            csv.Num(i, timesNum.printInterval) = -myEnvironment.OutputInterval
        Else
            csv.Num(i, timesNum.printInterval) = myEnvironment.OutputInterval
        End If
        csv.Num(i, timesNum.historyInterval) = myEnvironment.BinaryOutputInterval
        csv.Num(i, timesNum.smokeviewInterval) = myEnvironment.SmokeviewInterval
        csv.Num(i, timesNum.spreadsheetInterval) = myEnvironment.SpreadsheetInterval
        i += 1
        'Exterior ambient conditions
        csv.str(i, CFASTlnNum.keyWord) = "EAMB"
        csv.Num(i, ambNum.ambTemp) = myEnvironment.ExtAmbTemperature
        csv.Num(i, ambNum.ambPress) = myEnvironment.ExtAmbPressure
        csv.Num(i, ambNum.refHeight) = myEnvironment.ExtAmbElevation
        i += 1
        'Interior ambient conditions
        csv.str(i, CFASTlnNum.keyWord) = "TAMB"
        csv.Num(i, ambNum.ambTemp) = myEnvironment.IntAmbTemperature
        csv.Num(i, ambNum.ambPress) = myEnvironment.IntAmbPressure
        csv.Num(i, ambNum.refHeight) = myEnvironment.IntAmbElevation
        csv.Num(i, ambNum.relHumidity) = myEnvironment.IntAmbRH
        i += 1
        'Oxygen limit
        'csv.str(i, CFASTlnNum.keyWord) = "LIMO2"
        'csv.Num(i, 2) = myEnvironment.LowerOxygenLimit
        'i += 1
        'ceiling jet
        csv.str(i, CFASTlnNum.keyWord) = "CJET"
        If myEnvironment.CeilingJet = 0 Then csv.str(i, cjetNum.type) = "OFF"
        If myEnvironment.CeilingJet = 1 Then csv.str(i, cjetNum.type) = "CEILING"
        If myEnvironment.CeilingJet = 2 Then csv.str(i, cjetNum.type) = "WALLS"
        If myEnvironment.CeilingJet = 3 Then csv.str(i, cjetNum.type) = "ALL"
        i += 1
        'door jet ignition temperature
        'csv.str(i, CFASTlnNum.keyWord) = "DJIGN"
        'csv.Num(i, djignNum.igntemp) = myEnvironment.IgnitionTemp
        'i += 1
        ' global chemistry parameters (LIMO2 + DJIGN + CJET)
        csv.str(i, CFASTlnNum.keyWord) = "CHEMI"
        csv.Num(i, chemieNum.limo2) = myEnvironment.LowerOxygenLimit
        csv.Num(i, chemieNum.igntemp) = myEnvironment.IgnitionTemp
        i += 1
        'wind
        csv.str(i, CFASTlnNum.keyWord) = "WIND"
        csv.Num(i, windNum.velocity) = myEnvironment.ExtWindSpeed
        csv.Num(i, windNum.refHeight) = myEnvironment.ExtScaleHeight
        csv.Num(i, windNum.expLapseRate) = myEnvironment.ExtPowerLawCoefficient
        i += 1
        'comment header of compartment section
        If myCompartments.Count > 0 Then AddHeadertoOutput(csv, i, "Compartment keywords")
        'compartments
        Dim aCompartment As Compartment
        For j = 0 To myCompartments.Count - 1
            aCompartment = myCompartments.Item(j)
            csv.str(i, CFASTlnNum.keyWord) = "COMPA"
            csv.str(i, compaNum.Name) = aCompartment.Name
            aCompartment.GetSize(csv.Num(i, compaNum.Width), csv.Num(i, compaNum.Depth), csv.Num(i, compaNum.Height))
            aCompartment.GetPosition(csv.Num(i, compaNum.AbsXPos), csv.Num(i, compaNum.AbsYPos), _
                    csv.Num(i, compaNum.FlrHeight))
            aCompartment.GetMaterial(csv.str(i, compaNum.CeilingMat), csv.str(i, compaNum.WallMat), _
                    csv.str(i, compaNum.FloorMat))
            If csv.str(i, compaNum.CeilingMat) = "Off" Then csv.str(i, compaNum.CeilingMat) = "OFF"
            If csv.str(i, compaNum.WallMat) = "Off" Then csv.str(i, compaNum.WallMat) = "OFF"
            If csv.str(i, compaNum.FloorMat) = "Off" Then csv.str(i, compaNum.FloorMat) = "OFF"
            aCompartment.Changed = False
            i += 1
        Next
        'hall
        Dim hall As Boolean
        For j = 0 To myCompartments.Count - 1
            aCompartment = myCompartments.Item(j)
            If aCompartment.Hall Then
                csv.str(i, CFASTlnNum.keyWord) = "HALL"
                csv.Num(i, hallNum.compartment) = j + 1
                aCompartment.GetFlowType(hall, csv.Num(i, hallNum.vel), csv.Num(i, hallNum.depth), _
                    csv.Num(i, hallNum.DecayDist))
                i += 1
            End If
        Next
        'shaft
        For j = 0 To myCompartments.Count - 1
            aCompartment = myCompartments.Item(j)
            If aCompartment.Shaft Then
                csv.str(i, CFASTlnNum.keyWord) = "ONEZ"
                csv.Num(i, hallNum.compartment) = j + 1
                i += 1
            End If
        Next
        'RoomA and RoomH
        Dim x() As Single
        Dim k As Integer
        For j = 0 To myCompartments.Count - 1
            aCompartment = myCompartments.Item(j)
            aCompartment.GetVariableAreaPoints(x)
            If x.GetUpperBound(0) > 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "ROOMA"
                csv.Num(i, 2) = j + 1
                csv.Num(i, 3) = x.GetUpperBound(0)
                For k = 1 To x.GetUpperBound(0)
                    csv.Num(i, k + 3) = x(k)
                Next
                i += 1
                csv.str(i, CFASTlnNum.keyWord) = "ROOMH"
                aCompartment.GetVariableAreasHeight(x)
                csv.Num(i, 2) = j + 1
                csv.Num(i, 3) = x.GetUpperBound(0)
                For k = 1 To x.GetUpperBound(0)
                    csv.Num(i, k + 3) = x(k)
                Next
                i += 1
            End If
        Next
        'comment header for vents
        If myVVents.Count > 0 Or myHVents.Count > 0 Or myMVents.Count > 0 Then AddHeadertoOutput(csv, i, "vent keywords")
        Dim aVent As Vent
        'horizontal flow
        For j = 0 To myHVents.Count - 1
            csv.str(i, CFASTlnNum.keyWord) = "HVENT"
            aVent = myHVents.Item(j)
            csv.Num(i, hventNum.firstcomp) = aVent.FirstCompartment + 1
            csv.Num(i, hventNum.secondcomp) = aVent.SecondCompartment + 1
            If csv.Num(i, hventNum.firstcomp) = 0 Then _
                csv.Num(i, hventNum.firstcomp) = myCompartments.Count + 1
            If csv.Num(i, hventNum.secondcomp) = 0 Then _
                csv.Num(i, hventNum.secondcomp) = myCompartments.Count + 1
            csv.Num(i, hventNum.width) = aVent.Width
            csv.Num(i, hventNum.sill) = aVent.Sill
            csv.Num(i, hventNum.soffit) = aVent.Soffit
            csv.Num(i, hventNum.wind) = aVent.WindCosine
            csv.Num(i, hventNum.hall1) = aVent.FirstOffset
            csv.Num(i, hventNum.hall2) = aVent.SecondOffset
            csv.str(i, hventNum.face) = aVent.Face
            csv.Num(i, hventNum.initialfraction) = aVent.InitialOpening
            csv.Num(i, hventNum.vent) = myHVents.VentNumber(j)
            aVent.Changed = False
            i += 1
        Next
        'vertical flow
        For j = 0 To myVVents.Count - 1
            aVent = myVVents.Item(j)
            csv.str(i, CFASTlnNum.keyWord) = "VVENT"
            csv.Num(i, vventNum.firstcompartment) = aVent.FirstCompartment + 1
            csv.Num(i, vventNum.secondcompartment) = aVent.SecondCompartment + 1
            csv.Num(i, vventNum.area) = aVent.Area
            csv.Num(i, vventNum.shape) = aVent.Shape
            csv.Num(i, vventNum.intialfraction) = aVent.InitialOpening
            If csv.Num(i, vventNum.firstcompartment) = 0 Then _
                csv.Num(i, vventNum.firstcompartment) = myCompartments.Count + 1
            If csv.Num(i, vventNum.secondcompartment) = 0 Then _
                csv.Num(i, vventNum.secondcompartment) = myCompartments.Count + 1
            aVent.Changed = False
            i += 1
        Next
        'mvent
        For j = 0 To myMVents.Count - 1
            csv.str(i, CFASTlnNum.keyWord) = "MVENT"
            aVent = myMVents.Item(j)
            aVent.GetVent(csv.Num(i, mventNum.fromCompartment), csv.Num(i, mventNum.fromArea), _
                csv.Num(i, mventNum.fromHeight), csv.str(i, mventNum.fromOpenOrien), csv.Num(i, mventNum.toCompartment), _
                csv.Num(i, mventNum.toArea), csv.Num(i, mventNum.toHeight), csv.str(i, mventNum.toOpenOrien), _
                csv.Num(i, mventNum.flow), csv.Num(i, mventNum.beginFlowDrop), csv.Num(i, mventNum.flowZero))
            csv.Num(i, mventNum.fromCompartment) += 1
            If csv.Num(i, mventNum.fromCompartment) = 0 Then _
                csv.Num(i, mventNum.fromCompartment) = myCompartments.Count + 1
            csv.Num(i, mventNum.toCompartment) += 1
            If csv.Num(i, mventNum.toCompartment) = 0 Then csv.Num(i, mventNum.toCompartment) = myCompartments.Count + 1
            csv.Num(i, mventNum.IDNumber) = j + 1
            csv.Num(i, mventNum.initialfraction) = aVent.InitialOpening
            aVent.Changed = False
            i += 1
        Next
        'events
        For j = 0 To myHVents.Count - 1
            aVent = myHVents.Item(j)
            If aVent.FinalOpeningTime > 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "EVENT"
                csv.str(i, eventNum.ventType) = "H"
                csv.Num(i, eventNum.firstCompartment) = aVent.FirstCompartment + 1
                If csv.Num(i, eventNum.firstCompartment) = 0 Then _
                    csv.Num(i, eventNum.firstCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.secondCompartment) = aVent.SecondCompartment + 1
                If csv.Num(i, eventNum.secondCompartment) = 0 Then _
                    csv.Num(i, eventNum.secondCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.ventNumber) = myHVents.VentNumber(j)
                csv.Num(i, eventNum.time) = aVent.FinalOpeningTime
                csv.Num(i, eventNum.finalFraction) = aVent.FinalOpening
                csv.Num(i, eventNum.decaytime) = 1.0
                i += 1
            End If
        Next
        For j = 0 To myVVents.Count - 1
            aVent = myVVents.Item(j)
            If aVent.FinalOpeningTime > 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "EVENT"
                csv.str(i, eventNum.ventType) = "V"
                csv.Num(i, eventNum.firstCompartment) = aVent.FirstCompartment + 1
                If csv.Num(i, eventNum.firstCompartment) = 0 Then _
                    csv.Num(i, eventNum.firstCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.secondCompartment) = aVent.SecondCompartment + 1
                If csv.Num(i, eventNum.secondCompartment) = 0 Then _
                    csv.Num(i, eventNum.secondCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.ventNumber) = myVVents.VentNumber(j)
                csv.Num(i, eventNum.time) = aVent.FinalOpeningTime
                csv.Num(i, eventNum.finalFraction) = aVent.FinalOpening
                csv.Num(i, eventNum.decaytime) = 1.0
                i += 1
            End If
        Next
        For j = 0 To myMVents.Count - 1
            aVent = myMVents.Item(j)
            ' Mechanical ventilation vent opening fraction and time
            If aVent.FinalOpeningTime > 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "EVENT"
                csv.str(i, eventNum.ventType) = "M"
                csv.Num(i, eventNum.firstCompartment) = aVent.FirstCompartment + 1
                If csv.Num(i, eventNum.firstCompartment) = 0 Then _
                    csv.Num(i, eventNum.firstCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.secondCompartment) = aVent.SecondCompartment + 1
                If csv.Num(i, eventNum.secondCompartment) = 0 Then _
                    csv.Num(i, eventNum.secondCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.ventNumber) = j + 1
                csv.Num(i, eventNum.time) = aVent.FinalOpeningTime
                csv.Num(i, eventNum.finalFraction) = aVent.FinalOpening
                csv.Num(i, eventNum.decaytime) = 1.0
                i += 1
            End If
            ' Mechanical ventilation filtering fraction and time
            If aVent.FilterEfficiency <> 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "EVENT"
                csv.str(i, eventNum.ventType) = "F"
                csv.Num(i, eventNum.firstCompartment) = aVent.FirstCompartment + 1
                If csv.Num(i, eventNum.firstCompartment) = 0 Then _
                    csv.Num(i, eventNum.firstCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.secondCompartment) = aVent.SecondCompartment + 1
                If csv.Num(i, eventNum.secondCompartment) = 0 Then _
                    csv.Num(i, eventNum.secondCompartment) = myCompartments.Count + 1
                csv.Num(i, eventNum.ventNumber) = myMVents.VentNumber(j)
                csv.Num(i, eventNum.time) = aVent.FilterTime
                csv.Num(i, eventNum.filterEfficiency) = aVent.FilterEfficiency / 100.0
                csv.Num(i, eventNum.decaytime) = 1.0
                i += 1
            End If
        Next
        'comment header for fire keywords
        If myFires.Count > 0 Then AddHeadertoOutput(csv, i, "fire keywords")
        'mainf
        Dim aFire As New Fire
        For j = 0 To myFires.Count - 1
            aFire = myFires.Item(j)
            If aFire.Name = "mainfire" And aFire.FireObject >= 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "MAINF"
                csv.Num(i, fireNum.compartment) = aFire.Compartment + 1
                csv.Num(i, fireNum.xPosition) = aFire.XPosition
                csv.Num(i, fireNum.yPosition) = aFire.YPosition
                csv.Num(i, fireNum.zposition) = aFire.ZPosition
                csv.Num(i, fireNum.plumeType) = aFire.PlumeType + 1
                aFire.Changed = False
                i += 1
            End If
        Next
        'objects
        For j = 0 To myFires.Count - 1
            aFire = myFires.Item(j)
            If aFire.Name <> "mainfire" And aFire.FireObject >= 0 Then
                csv.str(i, CFASTlnNum.keyWord) = "OBJECT"
                csv.str(i, objfireNum.name) = aFire.Name
                csv.Num(i, objfireNum.compartment) = aFire.Compartment + 1
                csv.Num(i, objfireNum.xPosition) = aFire.XPosition
                csv.Num(i, objfireNum.yPosition) = aFire.YPosition
                csv.Num(i, objfireNum.zposition) = aFire.ZPosition
                csv.Num(i, objfireNum.xNormal) = aFire.XNormal
                csv.Num(i, objfireNum.yNormal) = aFire.YNormal
                csv.Num(i, objfireNum.zNormal) = aFire.ZNormal
                csv.Num(i, objfireNum.plumeType) = aFire.PlumeType + 1
                csv.Num(i, objfireNum.ignType) = aFire.IgnitionType + 1
                csv.Num(i, objfireNum.ignCriteria) = aFire.IgnitionValue
                aFire.Changed = False
                i += 1
            End If
        Next
        'comment header for heat transfer section
        If myHHeats.Count > 0 Or myVHeats.Count > 0 Then AddHeadertoOutput(csv, i, "heat flow keywords")
        'HHeat and VHeat
        For j = 0 To myHHeats.Count - 1
            aVent = myHHeats.Item(j)
            csv.str(i, CFASTlnNum.keyWord) = "HHEAT"
            csv.Num(i, hheatNum.firstCompartment) = aVent.FirstCompartment + 1
            csv.Num(i, hheatNum.num) = 1
            If csv.Num(i, hheatNum.secondCompartment) = -1 Then
                csv.Num(i, hheatNum.secondCompartment) = myCompartments.Count + 1
            Else
                csv.Num(i, hheatNum.secondCompartment) = aVent.SecondCompartment + 1
            End If
            csv.Num(i, hheatNum.fraction) = aVent.InitialOpening
            aVent.Changed = False
            i += 1
        Next
        For j = 0 To myVHeats.Count - 1
            aVent = myVHeats.Item(j)
            csv.str(i, CFASTlnNum.keyWord) = "VHEAT"
            csv.Num(i, vheatNum.firstcompartment) = aVent.FirstCompartment + 1
            csv.Num(i, vheatNum.secondcompartment) = aVent.SecondCompartment + 1
            If csv.Num(i, vheatNum.firstcompartment) = 0 Then _
                                            csv.Num(i, vheatNum.firstcompartment) = myCompartments.Count + 1
            If csv.Num(i, vheatNum.secondcompartment) = 0 Then _
                csv.Num(i, vheatNum.secondcompartment) = myCompartments.Count + 1
            aVent.Changed = False
            i += 1
        Next
        'comment header of targets and detectors
        If myDetectors.Count > 0 Or myTargets.Count > 0 Then AddHeadertoOutput(csv, i, "target and detector keywords")
        'detectors
        Dim aDetect As New Target
        For j = 0 To myDetectors.Count - 1
            csv.str(i, CFASTlnNum.keyWord) = "DETECT"
            aDetect = myDetectors.Item(j)
            If aDetect.DetectorType = Target.TypeSmokeDetector Then
                csv.Num(i, detectNum.type) = 1
                csv.Num(i, detectNum.suppression) = 0
            ElseIf aDetect.DetectorType = Target.TypeHeatDetector Then
                csv.Num(i, detectNum.type) = 2
                csv.Num(i, detectNum.suppression) = 0
            Else
                csv.Num(i, detectNum.type) = 2
                csv.Num(i, detectNum.suppression) = 1
            End If
            csv.Num(i, detectNum.compartment) = aDetect.Compartment + 1
            csv.Num(i, detectNum.activationTemp) = aDetect.ActivationTemperature
            csv.Num(i, detectNum.xPosition) = aDetect.XPosition
            csv.Num(i, detectNum.yPosition) = aDetect.YPosition
            csv.Num(i, detectNum.zPosition) = aDetect.ZPosition
            csv.Num(i, detectNum.RTI) = aDetect.RTI
            csv.Num(i, detectNum.sprayDensity) = aDetect.SprayDensity
            aDetect.Changed = False
            i += 1
        Next
        'Targets
        For j = 0 To myTargets.Count - 1
            aDetect = myTargets.Item(j)
            csv.str(i, CFASTlnNum.keyWord) = "TARGET"
            csv.Num(i, targetNum.compartment) = aDetect.Compartment + 1
            csv.Num(i, targetNum.xPosition) = aDetect.XPosition
            csv.Num(i, targetNum.yPosition) = aDetect.YPosition
            csv.Num(i, targetNum.zPosition) = aDetect.ZPosition
            csv.Num(i, targetNum.xNormal) = aDetect.XNormal
            csv.Num(i, targetNum.yNormal) = aDetect.YNormal
            csv.Num(i, targetNum.zNormal) = aDetect.ZNormal
            csv.str(i, targetNum.material) = aDetect.Material
            If aDetect.SolutionThickness = 1 Then
                csv.str(i, targetNum.equationType) = "ODE"
            Else
                csv.str(i, targetNum.equationType) = "PDE"
            End If
            If aDetect.SolutionMethod = 2 Then
                csv.str(i, targetNum.method) = "STEADY"
            ElseIf aDetect.SolutionMethod = 1 Then
                csv.str(i, targetNum.method) = "EXPLICIT"
            Else
                csv.str(i, targetNum.method) = "IMPLICIT"
            End If
            aDetect.Changed = False
            i += 1
        Next
        'comment header of misc.
        If myEnvironment.MaximumTimeStep > 0 Or myThermalProperties.FileName <> "thermal" Then _
            AddHeadertoOutput(csv, i, "misc. stuff")
        'stepmax
        If myEnvironment.MaximumTimeStep > 0 Then
            csv.str(i, CFASTlnNum.keyWord) = "STPMAX"
            csv.Num(i, 2) = myEnvironment.MaximumTimeStep
            i += 1
        End If
        'thermf
        If myThermalProperties.FileName <> "thermal" Then
            csv.str(i, CFASTlnNum.keyWord) = "THRMF"
            csv.str(i, 2) = myThermalProperties.FileName
            i += 1
        End If
        myEnvironment.Changed = False
        'comments and dead keywords
        If dataFileComments.Count > 0 Then AddHeadertoOutput(csv, i, "comments and ignore keywords")
        'comments
        For j = 1 To dataFileComments.Count
            csv.CSVrow(i) = dataFileComments.Item(j)
            i += 1
        Next

        csv.WrtCSVfile(FileName)

    End Sub
    Public Sub AddHeadertoOutput(ByRef csv As CSVsheet, ByRef i As Integer, ByVal header As String)
        csv.str(i, CFASTlnNum.keyWord) = "!!"
        i += 1
        csv.str(i, CFASTlnNum.keyWord) = "!!" + header
        i += 1
        csv.str(i, CFASTlnNum.keyWord) = "!!"
        i += 1
    End Sub
    Public Sub ReadInputFile(ByVal Filename As String)
        'Read in a *.in file Filename is to include path as well as file name
        Dim csv As New CSVsheet(Filename)
        Dim i As Integer = 1

        myErrors.Break()
        ' do loop three times to make sure that compartments are all entered and the vents are there for event
        Do Until i > csv.MaxRow
            If Not SkipLine(csv.str(i, CFASTlnNum.keyWord)) Then
                If "COMPA" = csv.str(i, CFASTlnNum.keyWord) Then
                    Dim compa As New Compartment
                    myCompartments.Add(compa)
                    compa.Name = csv.str(i, compaNum.Name)
                    compa.SetSize(csv.Num(i, compaNum.Width), csv.Num(i, compaNum.Depth), csv.Num(i, compaNum.Height))
                    compa.SetPosition(csv.Num(i, compaNum.AbsXPos), csv.Num(i, compaNum.AbsYPos), _
                            csv.Num(i, compaNum.FlrHeight))
                    compa.SetMaterial(csv.str(i, compaNum.CeilingMat), csv.str(i, compaNum.WallMat), _
                            csv.str(i, compaNum.FloorMat))
                    compa.Changed = False
                End If
            End If
            i += 1
        Loop

        ' do other keywords
        i = 1
        Do Until i > csv.MaxRow
            If Not SkipLine(csv.str(i, CFASTlnNum.keyWord)) Then
                Select Case csv.str(i, CFASTlnNum.keyWord).Trim
                    Case "VERSN"
                        myEnvironment.Title = csv.str(i, CFASTlnNum.title)
                        myEnvironment.Changed = False
                    Case "CHEMI"
                        myEnvironment.LowerOxygenLimit = csv.Num(i, chemieNum.limo2)
                        myEnvironment.IgnitionTemp = csv.Num(i, chemieNum.igntemp)
                    Case "CJET"
                        Dim iCjet As Integer
                        iCjet = InStr(CJetNames, csv.str(i, cjetNum.type)) / 7
                        If iCjet >= 0 And iCjet <= 3 Then
                            If iCjet = 3 Then iCjet = 2
                            myEnvironment.CeilingJet = iCjet
                        End If
                    Case "COMPA"        'Done in first loop
                    Case "DETECT"
                        Dim aDetect As New Target
                        aDetect.Type = Target.TypeDetector
                        If csv.Num(i, detectNum.type) = 1 Then
                            aDetect.DetectorType = Target.TypeSmokeDetector
                        ElseIf csv.Num(i, detectNum.suppression) = 1 Then
                            aDetect.DetectorType = Target.TypeSprinkler
                        Else
                            aDetect.DetectorType = Target.TypeHeatDetector
                        End If
                        aDetect.Compartment = csv.Num(i, detectNum.compartment) - 1
                        aDetect.ActivationTemperature = csv.Num(i, detectNum.activationTemp)
                        aDetect.XPosition = csv.Num(i, detectNum.xPosition)
                        aDetect.YPosition = csv.Num(i, detectNum.yPosition)
                        aDetect.ZPosition = csv.Num(i, detectNum.zPosition)
                        aDetect.RTI = csv.Num(i, detectNum.RTI)
                        aDetect.SprayDensity = csv.Num(i, detectNum.sprayDensity)
                        aDetect.Changed = False
                        myDetectors.Add(aDetect)
                    Case "DJIGN"
                        myEnvironment.IgnitionTemp = csv.Num(i, djignNum.igntemp)
                    Case "DTCHECK"              'ignored for now
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword DTCHECK not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "EAMB"
                        myEnvironment.ExtAmbTemperature = csv.Num(i, ambNum.ambTemp)
                        myEnvironment.ExtAmbPressure = csv.Num(i, ambNum.ambPress)
                        myEnvironment.ExtAmbElevation = csv.Num(i, ambNum.refHeight)
                        myEnvironment.Changed = False
                    Case "EVENT"
                    Case "HALL"
                        If csv.Num(i, hallNum.compartment) <= myCompartments.Count Then
                            Dim j As Integer = csv.Num(i, hallNum.compartment) - 1
                            If myCompartments(j).Shaft Then
                                myErrors.Add("Keyword HALL compartment  " + csv.str(i, hallNum.compartment) + " is already declared an one zone compartment and will be changed to a hall ", ErrorMessages.TypeError)
                            End If
                            myCompartments(j).setflowtype(True, csv.Num(i, hallNum.vel), csv.Num(i, hallNum.depth), _
                                csv.Num(i, hallNum.DecayDist))
                            myCompartments(j).Changed = False
                        End If
                    Case "HHEAT"
                        If csv.Num(i, hheatNum.num) = 1 Then
                            Dim aHeat As New Vent
                            If csv.Num(i, hheatNum.secondCompartment) > myCompartments.Count Then _
                                csv.Num(i, hheatNum.secondCompartment) = 0
                            aHeat.SetVent(csv.Num(i, hheatNum.firstCompartment) - 1, csv.Num(i, hheatNum.secondCompartment) - 1, _
                            csv.Num(i, hheatNum.fraction))
                            aHeat.Changed = False
                            myHHeats.Add(aHeat)
                        ElseIf csv.Num(i, hheatNum.num) = 0 Then
                            dataFileComments.Add("!" + csv.strrow(i))
                            myErrors.Add("Keyword HHEAT with single compartment specification not supported line" + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                        Else
                            dataFileComments.Add("!" + csv.strrow(i))
                            myErrors.Add("Keyword HHEAT with multiple fraction specifications not supported line" + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                        End If
                    Case "HVENT"
                        Dim hvent As New Vent
                        If csv.Num(i, hventNum.firstcomp) > myCompartments.Count Then _
                            csv.Num(i, hventNum.firstcomp) = 0
                        If csv.Num(i, hventNum.secondcomp) > myCompartments.Count Then _
                            csv.Num(i, hventNum.secondcomp) = 0
                        hvent.SetVent(csv.Num(i, hventNum.firstcomp) - 1, csv.Num(i, hventNum.secondcomp) - 1, _
                            csv.Num(i, hventNum.width), csv.Num(i, hventNum.soffit), csv.Num(i, hventNum.sill))
                        hvent.WindCosine = csv.Num(i, hventNum.wind)
                        hvent.FirstOffset = csv.Num(i, hventNum.hall1)
                        hvent.SecondOffset = csv.Num(i, hventNum.hall2)
                        hvent.Face = csv.str(i, hventNum.face)
                        hvent.InitialOpening = csv.Num(i, hventNum.initialfraction)
                        hvent.FinalOpening = csv.Num(i, hventNum.initialfraction)
                        hvent.Changed = False
                        myHVents.Add(hvent)
                    Case "INTER"        'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword INTER not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "LFBO"         'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword LFBO not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "LFBT"         'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword LFBT not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "LIMO2"
                        myEnvironment.LowerOxygenLimit = csv.Num(i, 2)
                        myEnvironment.Changed = False
                    Case "MAINF"
                        Dim aFire As New Fire
                        aFire.Name = "mainfire"
                        aFire.SetPosition(csv.Num(i, fireNum.compartment) - 1, csv.Num(i, fireNum.xPosition), _
                            csv.Num(i, fireNum.yPosition), csv.Num(i, fireNum.zposition))
                        aFire.PlumeType = csv.Num(i, fireNum.plumeType) - 1
                        aFire.FireObject = myFireObjects.GetFireIndex(aFire.Name)
                        aFire.Changed = False
                        myFires.Add(aFire)
                    Case "MVFAN"        'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword MVFAN not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "MVDCT"        'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword MVDCT not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "MVENT"
                        Dim mvent As New Vent
                        If csv.Num(i, mventNum.fromCompartment) > myCompartments.Count Then _
                            csv.Num(i, mventNum.fromCompartment) = 0
                        If csv.Num(i, mventNum.toCompartment) > myCompartments.Count Then _
                            csv.Num(i, mventNum.toCompartment) = 0
                        mvent.SetVent(csv.Num(i, mventNum.fromCompartment) - 1, csv.Num(i, mventNum.fromArea), _
                            csv.Num(i, mventNum.fromHeight), csv.str(i, mventNum.fromOpenOrien), _
                            csv.Num(i, mventNum.toCompartment) - 1, csv.Num(i, mventNum.toArea), _
                            csv.Num(i, mventNum.toHeight), csv.str(i, mventNum.toOpenOrien), csv.Num(i, mventNum.flow), _
                            csv.Num(i, mventNum.beginFlowDrop), csv.Num(i, mventNum.flowZero))
                        mvent.InitialOpening = csv.Num(i, mventNum.initialfraction)
                        mvent.FinalOpening = csv.Num(i, mventNum.initialfraction)
                        mvent.Changed = False
                        myMVents.Add(mvent)
                    Case "OBJECT"
                        If myFireObjects.GetFireIndex(csv.str(i, objfireNum.name)) >= 0 Then
                            Dim aFire As New Fire
                            aFire.Name = csv.str(i, objfireNum.name)
                            aFire.SetPosition(csv.Num(i, objfireNum.compartment) - 1, csv.Num(i, objfireNum.xPosition), _
                                csv.Num(i, objfireNum.yPosition), csv.Num(i, objfireNum.zposition), _
                                csv.Num(i, objfireNum.xNormal), csv.Num(i, objfireNum.yNormal), csv.Num(i, objfireNum.zNormal))
                            aFire.PlumeType = csv.Num(i, objfireNum.plumeType) - 1
                            aFire.IgnitionType = csv.Num(i, objfireNum.ignType) - 1
                            aFire.IgnitionValue = csv.Num(i, objfireNum.ignCriteria)
                            aFire.FireObject = myFireObjects.GetFireIndex(aFire.Name)
                            aFire.Changed = False
                            myFires.Add(aFire)
                        Else
                            myErrors.Add("Fire Object " + csv.str(i, objfireNum.name) + " does not exist and will not be added to the simulation", ErrorMessages.TypeError)
                        End If
                    Case "OBJFL"        'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword OBJFL not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "ONEZ"
                        If csv.Num(i, 2) <= myCompartments.Count Then
                            If myCompartments(csv.Num(i, 2) - 1).Hall Then
                                myErrors.Add("Keyword ONEZ room  " + csv.str(i, 2) + " is already declared a hall and will be changed to a one zone compartment ", ErrorMessages.TypeError)
                            End If
                            myCompartments(csv.Num(i, 2) - 1).Shaft = True
                            myCompartments(csv.Num(i, 2) - 1).Changed = False
                        End If
                    Case "ROOMA"
                        If csv.Num(i, 2) <= myCompartments.Count Then
                            Dim aComp As Compartment = myCompartments(csv.Num(i, 2) - 1)
                            Dim area(csv.Num(i, 3)) As Single
                            Dim j As Integer
                            For j = 1 To csv.Num(i, 3)
                                area(j) = csv.Num(i, j + 3)
                            Next
                            aComp.SetVariableAreaPoints(area)
                            aComp.Changed = False
                        End If
                    Case "ROOMH"
                        If csv.Num(i, 2) <= myCompartments.Count Then
                            Dim aComp As Compartment = myCompartments(csv.Num(i, 2) - 1)
                            Dim height(csv.Num(i, 3)) As Single
                            Dim j As Integer
                            For j = 1 To csv.Num(i, 3)
                                height(j) = csv.Num(i, j + 3)
                            Next
                            aComp.SetVariableAreasHeight(height)
                            aComp.Changed = False
                        End If
                    Case "SETP"         'ignored
                        dataFileComments.Add("!" + csv.strrow(i))
                        myErrors.Add("Keyword SETP not supported line " + csv.strrow(i) + " will be commented out", ErrorMessages.TypeWarning)
                    Case "STPMAX"
                        myEnvironment.MaximumTimeStep = csv.Num(i, 2)
                        myEnvironment.Changed = False
                    Case "TAMB"
                        myEnvironment.IntAmbTemperature = csv.Num(i, ambNum.ambTemp)
                        myEnvironment.IntAmbPressure = csv.Num(i, ambNum.ambPress)
                        myEnvironment.IntAmbElevation = csv.Num(i, ambNum.refHeight)
                        myEnvironment.IntAmbRH = csv.Num(i, ambNum.relHumidity)
                        myEnvironment.Changed = False
                    Case "TARGET"
                        Dim aDetect As New Target
                        aDetect.Type = 0
                        aDetect.SetPosition(csv.Num(i, targetNum.xPosition), csv.Num(i, targetNum.yPosition), _
                            csv.Num(i, targetNum.zPosition), csv.Num(i, targetNum.xNormal), csv.Num(i, targetNum.yNormal), _
                            csv.Num(i, targetNum.zNormal))
                        Dim thickness, method As Integer
                        If csv.str(i, targetNum.equationType) = "ODE" Then
                            thickness = 1
                        Else
                            thickness = 0
                        End If
                        If csv.str(i, targetNum.method) = "STEADY" Then
                            method = 2
                        ElseIf csv.str(i, targetNum.method) = "EXPLICIT" Then
                            method = 1
                        Else
                            method = 0
                        End If
                        aDetect.SetTarget(csv.Num(i, targetNum.compartment) - 1, csv.str(i, targetNum.material), thickness, _
                            method)
                        aDetect.Changed = False
                        myTargets.Add(aDetect)
                    Case "THRMF"
                        myThermalProperties.Clear()
                        ReadThermalProperties(".\" + csv.str(i, 2).Trim + ".csv")
                    Case "TIMES"
                        myEnvironment.SimulationTime = csv.Num(i, timesNum.simTime)
                        myEnvironment.OutputInterval = Math.Abs(csv.Num(i, timesNum.printInterval))
                        If csv.Num(i, timesNum.printInterval) < 0 Then
                            DetailedCFASTOutput = True
                        Else
                            DetailedCFASTOutput = False
                        End If
                        myEnvironment.BinaryOutputInterval = csv.Num(i, timesNum.historyInterval)
                        myEnvironment.SmokeviewInterval = csv.Num(i, timesNum.smokeviewInterval)
                        myEnvironment.SpreadsheetInterval = csv.Num(i, timesNum.spreadsheetInterval)
                        myEnvironment.Changed = False
                    Case "WIND"
                        myEnvironment.ExtWindSpeed = csv.Num(i, windNum.velocity)
                        myEnvironment.ExtScaleHeight = csv.Num(i, windNum.refHeight)
                        myEnvironment.ExtPowerLawCoefficient = csv.Num(i, windNum.expLapseRate)
                        myEnvironment.Changed = False
                    Case "VHEAT"
                        Dim vheat As New Vent
                        If csv.Num(i, vheatNum.firstcompartment) > myCompartments.Count Then _
                            csv.Num(i, vheatNum.firstcompartment) = 0
                        If csv.Num(i, vheatNum.secondcompartment) > myCompartments.Count Then _
                            csv.Num(i, vheatNum.secondcompartment) = 0
                        vheat.SetVent(csv.Num(i, vheatNum.firstcompartment) - 1, csv.Num(i, vheatNum.secondcompartment) - 1)
                        vheat.Changed = False
                        myVHeats.Add(vheat)
                    Case "VVENT"
                        Dim vvent As New Vent
                        If csv.Num(i, vventNum.firstcompartment) > myCompartments.Count Then _
                            csv.Num(i, vventNum.firstcompartment) = 0
                        If csv.Num(i, vventNum.secondcompartment) > myCompartments.Count Then _
                            csv.Num(i, vventNum.secondcompartment) = 0
                        vvent.SetVent(csv.Num(i, vventNum.firstcompartment) - 1, csv.Num(i, vventNum.secondcompartment) - 1, _
                            csv.Num(i, vventNum.area), csv.Num(i, vventNum.shape))
                        vvent.InitialOpening = csv.Num(i, vventNum.intialfraction)
                        vvent.FinalOpening = csv.Num(i, vventNum.intialfraction)
                        vvent.Changed = False
                        myVVents.Add(vvent)
                End Select
            Else
                If HeaderComment(csv.str(i, 1)) Then
                    dataFileHeader.Add(csv.strrow(i))
                ElseIf DropComment(csv.strrow(i)) Then
                    'drop the comment
                Else
                    dataFileComments.Add(csv.strrow(i))
                End If
            End If
            i += 1
        Loop
        ' do EVENT Keyword
        i = 1
        Do Until i > csv.MaxRow
            If Not SkipLine(csv.str(i, CFASTlnNum.keyWord)) Then
                If csv.str(i, CFASTlnNum.keyWord).Trim = "EVENT" Then
                    If csv.str(i, eventNum.ventType).Trim = "H" Then
                        If csv.Num(i, eventNum.firstCompartment) > myCompartments.Count Then csv.Num(i, eventNum.firstCompartment) = 0
                        If csv.Num(i, eventNum.secondCompartment) > myCompartments.Count Then csv.Num(i, eventNum.secondCompartment) = 0
                        Dim index As Integer = myHVents.GetIndex(csv.Num(i, eventNum.firstCompartment) - 1, _
                            csv.Num(i, eventNum.secondCompartment) - 1, csv.Num(i, eventNum.ventNumber))
                        If index > -1 Then
                            Dim aVent As Vent = myHVents.Item(index)
                            aVent.FinalOpeningTime = csv.Num(i, eventNum.time)
                            aVent.FinalOpening = csv.Num(i, eventNum.finalFraction)
                            aVent.Changed = False
                        Else
                            'error handling vent doesn't exist
                            myErrors.Add("Keyword EVENT Hvent " + csv.str(i, eventNum.ventNumber) + " between compartments " + csv.str(i, eventNum.firstCompartment) + " and " + csv.str(i, eventNum.secondCompartment) + " does not exist", ErrorMessages.TypeError)
                        End If
                    ElseIf csv.str(i, eventNum.ventType).Trim = "V" Then
                        If csv.Num(i, eventNum.firstCompartment) > myCompartments.Count Then csv.Num(i, eventNum.firstCompartment) = 0
                        If csv.Num(i, eventNum.secondCompartment) > myCompartments.Count Then csv.Num(i, eventNum.secondCompartment) = 0
                        Dim index As Integer = myVVents.GetIndex(csv.Num(i, eventNum.firstCompartment) - 1, _
                            csv.Num(i, eventNum.secondCompartment) - 1, csv.Num(i, eventNum.ventNumber))
                        If index > -1 Then
                            Dim aVent As Vent = myVVents.Item(index)
                            aVent.FinalOpeningTime = csv.Num(i, eventNum.time)
                            aVent.FinalOpening = csv.Num(i, eventNum.finalFraction)
                            aVent.Changed = False
                        Else
                            'error handling vent doesn't exist
                            myErrors.Add("Keyword EVENT Vvent " + csv.str(i, eventNum.ventNumber) + " between compartments " + csv.str(i, eventNum.firstCompartment) + " and " + csv.str(i, eventNum.secondCompartment) + " does not exist", ErrorMessages.TypeError)
                        End If
                    ElseIf csv.str(i, eventNum.ventType).Trim = "M" Then
                        If csv.Num(i, eventNum.firstCompartment) > myCompartments.Count Then csv.Num(i, eventNum.firstCompartment) = 0
                        If csv.Num(i, eventNum.secondCompartment) > myCompartments.Count Then csv.Num(i, eventNum.secondCompartment) = 0
                        Dim index As Integer = myMVents.GetIndex(csv.Num(i, eventNum.firstCompartment) - 1, _
                            csv.Num(i, eventNum.secondCompartment) - 1, csv.Num(i, eventNum.ventNumber))
                        If index > -1 Then
                            Dim aVent As Vent = myMVents.Item(index)
                            aVent.FinalOpeningTime = csv.Num(i, eventNum.time)
                            aVent.FinalOpening = csv.Num(i, eventNum.finalFraction)
                            aVent.Changed = False
                        Else
                            'error handling vent doesn't exist
                            myErrors.Add("Keyword EVENT Mvent " + csv.str(i, eventNum.ventNumber) + " between compartments " + csv.str(i, eventNum.firstCompartment) + " and " + csv.str(i, eventNum.secondCompartment) + " does not exist", ErrorMessages.TypeError)
                        End If
                    ElseIf csv.str(i, eventNum.ventType).Trim = "F" Then
                        If csv.Num(i, eventNum.firstCompartment) > myCompartments.Count Then csv.Num(i, eventNum.firstCompartment) = 0
                        If csv.Num(i, eventNum.secondCompartment) > myCompartments.Count Then csv.Num(i, eventNum.secondCompartment) = 0
                        Dim index As Integer = myMVents.GetIndex(csv.Num(i, eventNum.firstCompartment) - 1, _
                            csv.Num(i, eventNum.secondCompartment) - 1, csv.Num(i, eventNum.ventNumber))
                        If index > -1 Then
                            Dim aVent As Vent = myMVents.Item(index)
                            aVent.FilterTime = csv.Num(i, eventNum.time)
                            aVent.FilterEfficiency = csv.Num(i, eventNum.finalFraction) * 100.0
                            aVent.Changed = False
                        Else
                            'error handling vent doesn't exist
                            myErrors.Add("Keyword EVENT Mvent Filter " + csv.str(i, eventNum.ventNumber) + " between compartments " + csv.str(i, eventNum.firstCompartment) + " and " + csv.str(i, eventNum.secondCompartment) + " does not exist", ErrorMessages.TypeError)
                        End If
                    Else
                        'error handling wrong vent types
                        myErrors.Add("Keyword EVENT vent type " + csv.str(i, eventNum.ventType) + " is not recognized", ErrorMessages.TypeError)
                    End If
                End If
            End If
            i += 1
        Loop
    End Sub
    Public Function SkipLine(ByVal str As String) As Boolean
        If str = Nothing Then
            Return True
        End If
        If str.Length = 0 Then
            Return True
        ElseIf str.Substring(0, 1) = "!" Or str.Substring(0, 1) = "#" Then
            Return True
        Else
            Return False
        End If
    End Function
    Public Function HeaderComment(ByVal str As String) As Boolean
        If str = Nothing Then
            Return False
        End If
        If str.Length > 1 Then
            If str.Substring(0, 2) = "!*" Then
                Return True
            End If
        Else
            Return False
        End If
    End Function
    Public Function Comment(ByVal str As String) As Boolean
        If str = Nothing Then
            Return True
        End If
        If str.Length > 0 Then
            If str.Substring(0, 1) = "!" Then
                Return True
            End If
        Else
            Return False
        End If
    End Function
    Public Function DropComment(ByVal str As String) As Boolean
        If str = Nothing Then
            Return True
        End If
        If str.Length > 1 Then
            If str.Substring(0, 2) = "!!" Then
                Return True
            End If
        ElseIf str.Length = 0 Then
            Return True
        Else
            Return False
        End If
    End Function
    Public Function GetCommandLineArg() As String
        ' Declare variables.
        Dim command As String = Microsoft.VisualBasic.Command()
        If command.Length > 0 Then
            If command.Substring(0, 1) = """" Then
                command = command.Remove(0, 1)
                command = command.Remove(command.Length - 1, 1)
            End If
        End If
        Return command
    End Function
End Module
