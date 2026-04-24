// SEMF - Space Engineers Management Framework
// https://malforge.github.io/spaceengineers/pbapi/
int CurrentBlockCount, PastBlockCount;

int LightRadius;

string StorageMaterialsKeyword;
string StorageComponentsKeyword;
string StorageConsumblesKeyword;
string StorageAmmoKeyword;
string StorageMaterialsDisplayKeyword;
string StorageComponentsDisplayKeyword;
string StorageConsumblesDisplayKeyword;
string StorageAmmoDisplayKeyword;
int StorageMaterialsDisplayPanel;
int StorageComponentsDisplayPanel;
int StorageConsumblesDisplayPanel;
int StorageAmmoDisplayPanel;

string ProbeCameraKeyword;
string ProbeDisplayKeyword;

string SolarPointerRCKeyword;
string solarAzimuthRotorKeyword;
string SolarDisplayKeyword;
string SolarCTCKeyword;
string SolarCameraKeyword;
int SolarDisplaypanel;
int SolarRedirects;
double Proximity, SolarDirectionSwitch;
double SolarPowerOutput, SolarPowerOutputPrev;

////////////////////////////////////// LISTS ///////////////////////////////////
Dictionary<string, string> assemblerBlueprintMap = new Dictionary<string, string>();

List<IMyTerminalBlock> L = new List<IMyTerminalBlock>();
List<MyInventoryItem> items = new List<MyInventoryItem>();

List<IMyReflectorLight> ReflectorLightBlocks = new List<IMyReflectorLight>();
List<IMyInteriorLight> InteriorLightBlocks = new List<IMyInteriorLight>();
List<IMyLightingBlock> interiorLightsCast = new List<IMyLightingBlock>();
List<IMyLightingBlock> reflectorLightsCast = new List<IMyLightingBlock>();

List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyRefinery> refinerys = new List<IMyRefinery>();

List<IMyMedicalRoom> RefillPoints = new List<IMyMedicalRoom>();
List<IMyTextSurfaceProvider> Clocks = new List<IMyTextSurfaceProvider>(); // [Clocks]

List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
List<IMyFunctionalBlock> algaeFarms = new List<IMyFunctionalBlock>();

List<IMyTextSurfaceProvider> ProbeDisplay = new List<IMyTextSurfaceProvider>(); // [ScopeTarget]
////////////////////////////////////// LISTS ///////////////////////////////////
Color interiorLightColor;
Color reflectorLightColor;
Color foregroundColor;
Color backgroundColor;
const string font = "Monospace";
////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////
IMyTerminalBlock invMatBlock;
IMyTerminalBlock invCompBlock;
IMyTerminalBlock invConsumblesBlock;
IMyTerminalBlock invAmmoBlock;

IMyInventory MaterialsInventory;
IMyInventory ComponentsInventory;
IMyInventory ConsumblesInventory;
IMyInventory AmmoInventory;

IMyMotorStator solarAzimuthRotor;
IMyRemoteControl solarController;
IMyCameraBlock solarCamera;
IMyTextSurface solarDisplay;
IMyTurretControlBlock solarCTC;

IMyTextSurface materialsDisplay;
IMyTextSurface componentsDisplay;
IMyTextSurface ConsumblesDisplay;
IMyTextSurface ammoDisplay;

IMyCameraBlock ProbeCamera;

System.Text.StringBuilder _sb = new System.Text.StringBuilder();
////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////
int Ticker;
const int TickerLimit = 12;
double[] tickTimes = new double[TickerLimit];
double[] tickMaxTimes = new double[TickerLimit];
double[] tickInstructions = new double[TickerLimit];
DateTime tickStart;
public Program()
{
	CurrentBlockCount = PastBlockCount = 0;
	Runtime.UpdateFrequency = UpdateFrequency.Update10; //| UpdateFrequency.Update10 | UpdateFrequency.Update1
	Ticker = 0;
	// Light -----------------------------------------------------------------------
	interiorLightColor = new Color(204, 255, 140, 255);
	reflectorLightColor = new Color(255, 255, 255, 255);
	LightRadius = 10;
	// Clocks ----------------------------------------------------------------------
	foregroundColor = new Color(100, 255, 100, 255);
	backgroundColor = new Color(0, 0, 0, 255);
	// Storage ---------------------------------------------------------------------
	StorageMaterialsKeyword = "[Materials]";
	StorageMaterialsDisplayKeyword = "[MatDisplay]";
	StorageMaterialsDisplayPanel = 0;
	
	StorageComponentsKeyword = "[Components]";
	StorageComponentsDisplayKeyword = "[CompDisplay]";
	StorageComponentsDisplayPanel = 0;

	StorageConsumblesKeyword = "[Consumbles]";
	StorageConsumblesDisplayKeyword = "[ConsumblesDisplay]";
	StorageConsumblesDisplayPanel = 0;

	StorageAmmoKeyword = "[Ammo]";
	StorageAmmoDisplayKeyword = "[AmmoDisplay]";
	StorageAmmoDisplayPanel = 0;
	// Solar -----------------------------------------------------------------------
	SolarPointerRCKeyword = "[Axis Aligner]";
	solarAzimuthRotorKeyword = "[Solar Wing]";
	SolarDisplayKeyword = "[SolarDisplay]";
	SolarCTCKeyword = "[SolarCTC]";
	SolarCameraKeyword = "[SolarTracker]";

	SolarDisplaypanel = 0;
	SolarDirectionSwitch = 1;
	SolarRedirects = 0;
	Proximity = 0.001;

	// Probe -------------------------------------------------------------------
	ProbeCameraKeyword = "[ScopeCamera]";
	ProbeDisplayKeyword = "[ScopeTarget]";

	BlueprintMapFill();
}
//public void Save(){}
public void Main(string argument, UpdateType updateSource)
{
	if (argument.IndexOf("refresh", StringComparison.OrdinalIgnoreCase) >= 0)
	{
		RefreshBlockCache();
		return;
	}
	if (argument.IndexOf("navscan", StringComparison.OrdinalIgnoreCase) >= 0)
	{
		int distance = 20000;
		if (argument.Trim().Split(' ').Length >= 2) int.TryParse(argument.Trim().Split(' ')[1], out distance);
		AquireTarget(distance);
		return;
	}
	if (argument.IndexOf("cleanminer", StringComparison.OrdinalIgnoreCase) >= 0)
	{
		MoveGridContentsToInventory("Miner", MaterialsInventory);
		return;
	}
	if (argument.IndexOf("getasmqueue", StringComparison.OrdinalIgnoreCase) >= 0)
	{
		Me.CustomData = "getasmqueue";
		return;
	}
	if (argument.IndexOf("formatdisplay", StringComparison.OrdinalIgnoreCase) >= 0)
	{
		string keyword = argument.Trim().Split(' ')[1];
		FormatAllDisplays(BlockNamed(keyword) as IMyTextSurfaceProvider);
		return;
	}
// ------------------------------- MAIN UPDATE LOOP ---------------------------
	if ((updateSource & UpdateType.Update10) == 0) return;
	EchoStats();

	tickStart = DateTime.Now;
	switch (Ticker)
	{
		case 0: if (BlockCountChanged()) RefreshBlockCache(); break;
		case 1: CleanAssemblers(); SortComponents(); break;
		case 2: ProcessAssemblerQueue(invCompBlock); break;
		case 3: ProcessAssemblerQueue(invAmmoBlock); break;
		case 4:	DisplayStoredComponents(); break;
		case 5: DisplayStoredConsumbles(); break;
		case 6: DisplayStoredAmmo(); break;
		case 7: CleanAlgaeFarms(); CleanRefinerys(); break;
		case 8: DisplayStoredMaterials(); break;
		case 9: InteriorLightAdjust(); ReflectorLightAdjust(); break;
		case 10: SolarAdjust(); break;
		case 11: DisplayClocks(); break;
	}
	tickTimes[Ticker] = (DateTime.Now - tickStart).TotalMilliseconds;
	tickMaxTimes[Ticker] = Math.Max(tickTimes[Ticker], tickMaxTimes[Ticker]);
	tickInstructions[Ticker] = Runtime.CurrentInstructionCount;

	Ticker = (Ticker + 1) % TickerLimit;

}
// ------------------------------------------------------------------------------- Cache
void EchoStats()
{
	_sb.Clear();
	_sb.AppendLine("Execution Time Stats");
	_sb.AppendLine("[ T]:   ms ( max) | instr");
	for (int i = 0; i < TickerLimit; i++)
		_sb.AppendLine(string.Format("[{0,2}]: {1,4:F0} ({2,4:F0}) | {3,5}", i, tickTimes[i], tickMaxTimes[i], tickInstructions[i]));
	_sb.AppendLine(string.Format("Tick: {0}", Ticker));

	string text = _sb.ToString();
	Echo(text);
	Me.GetSurface(0).WriteText(text);
}
// ------------------------------------------------------------------------------- Cache
void RefreshBlockCache()
{
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refinerys);
	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);
	GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels);

	GridTerminalSystem.GetBlocksOfType<IMyMedicalRoom>(RefillPoints);
	Clocks.Clear();
	foreach (var block in BlocksNamed("[Clock]")) Clocks.Add(block as IMyTextSurfaceProvider);
	

	interiorLightsCast.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(InteriorLightBlocks);
	foreach (var light in InteriorLightBlocks) interiorLightsCast.Add(light);

	reflectorLightsCast.Clear();
	GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(ReflectorLightBlocks);
	foreach (var light in ReflectorLightBlocks) reflectorLightsCast.Add(light);

	invMatBlock        = BlockNamed(StorageMaterialsKeyword);
	invCompBlock       = BlockNamed(StorageComponentsKeyword);
	invConsumblesBlock = BlockNamed(StorageConsumblesKeyword);
	invAmmoBlock       = BlockNamed(StorageAmmoKeyword);

	MaterialsInventory 	= (invMatBlock != null) ? invMatBlock.GetInventory(0) : null;
	ComponentsInventory = (invCompBlock != null) ? invCompBlock.GetInventory(0) : null;
	ConsumblesInventory = (invConsumblesBlock != null) ? invConsumblesBlock.GetInventory(0) : null;
	AmmoInventory 			= (invAmmoBlock != null) ? invAmmoBlock.GetInventory(0) : null;

	solarAzimuthRotor  = BlockNamed(solarAzimuthRotorKeyword) as IMyMotorStator;
	solarController    = BlockNamed(SolarPointerRCKeyword) as IMyRemoteControl;
	solarCTC           = BlockNamed(SolarCTCKeyword) as IMyTurretControlBlock;
	solarCamera        = BlockNamed(SolarCameraKeyword) as IMyCameraBlock;

	ProbeCamera   = BlockNamed(ProbeCameraKeyword) as IMyCameraBlock;
	ProbeDisplay.Clear();
	foreach (var display in BlocksNamed(ProbeDisplayKeyword)) ProbeDisplay.Add(display as IMyTextSurfaceProvider);

	solarDisplay       = GetTextSurface(BlockNamed(SolarDisplayKeyword) as IMyTextSurfaceProvider, SolarDisplaypanel);
	materialsDisplay   = GetTextSurface(BlockNamed(StorageMaterialsDisplayKeyword) as IMyTextSurfaceProvider, StorageMaterialsDisplayPanel);
	componentsDisplay  = GetTextSurface(BlockNamed(StorageComponentsDisplayKeyword) as IMyTextSurfaceProvider, StorageComponentsDisplayPanel);
	ConsumblesDisplay  = GetTextSurface(BlockNamed(StorageConsumblesDisplayKeyword) as IMyTextSurfaceProvider, StorageConsumblesDisplayPanel);
	ammoDisplay        = GetTextSurface(BlockNamed(StorageAmmoDisplayKeyword) as IMyTextSurfaceProvider, StorageAmmoDisplayPanel);

	GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(algaeFarms, block => block.BlockDefinition.SubtypeId == "LargeBlockAlgaeFarm");
}
// ------------------------------------------------------------------------------- Light
void InteriorLightAdjust()
{
	LightAdjust(interiorLightsCast, interiorLightColor, LightRadius);
}
void ReflectorLightAdjust()
{
	LightAdjust(reflectorLightsCast, reflectorLightColor, 160);
}
void LightAdjust(List<IMyLightingBlock> LightBlocks, Color color, int LightRadius = 10, int intensity = 5, bool samegrid = true)
{
	for (int i = 0; i < LightBlocks.Count; i++)
	{
		if (samegrid && LightBlocks[i].CubeGrid != Me.CubeGrid) continue;
		LightBlocks[i].Radius = LightRadius;
		LightBlocks[i].Intensity = intensity;
		LightBlocks[i].Color = color;
	}
}
// ------------------------------------------------------------------------------- Clocks
void DisplayClocks()
{
	for (int i = 0; i < RefillPoints.Count; i++)
	{
		DisplayClock(RefillPoints[i] as IMyTextSurfaceProvider, 0);
	}
	for (int i = 0; i < Clocks.Count; i++)
	{
		DisplayClock(Clocks[i], 0);
	}
}
void DisplayClock(IMyTextSurfaceProvider DisplayBlock, int panel = 0)
{
	var surface = GetTextSurface(DisplayBlock, panel);
	if (surface != null)
	{
		surface.ContentType = ContentType.SCRIPT;
		surface.Script = "TSS_ClockDigital";
		surface.ScriptBackgroundColor = backgroundColor;
		surface.ScriptForegroundColor = foregroundColor;
	}
}
// ------------------------------------------------------------------------------- Storage
void DisplayStorageContents()
{
  DisplayStoredMaterials();
	DisplayStoredComponents();
	DisplayStoredConsumbles();
	DisplayStoredAmmo();
}
void DisplayStoredComponents()
{
	if (ComponentsInventory == null || componentsDisplay == null) return;
	DisplayStored(ComponentsInventory, componentsDisplay, "== Components ==");
}
void DisplayStoredMaterials()
{
	if (MaterialsInventory == null || materialsDisplay == null) return;
	DisplayStored(MaterialsInventory, materialsDisplay, "== Materials ==", "{0,-15}: {1,13:N2}");
}
void DisplayStoredConsumbles()
{
	if (ConsumblesInventory == null || ConsumblesDisplay == null) return;
	DisplayStored(ConsumblesInventory, ConsumblesDisplay, "== Consumbles ==");
}
void DisplayStoredAmmo()
{
	if (AmmoInventory == null || ammoDisplay == null) return;
	DisplayStored(AmmoInventory, ammoDisplay, "== Ammo ==","{0,-35}: {1,4:N0}");
}
void DisplayStored(IMyInventory inventory, IMyTextSurface display, string title, string format = "{0,-20}: {1,6:N0}")
{
	if (inventory != null && display != null)
	{
		display.Font = font;
		display.FontColor = foregroundColor;
		display.BackgroundColor = backgroundColor;
		items.Clear();
		_sb.Clear();
		_sb.AppendLine(title);
		inventory.GetItems(items);
		items.Sort((a, b) => ((double)b.Amount).CompareTo((double)a.Amount));
		for (int i = 0; i < items.Count; i++)
			_sb.AppendLine(string.Format(format, FormatTypeId(items[i]), (double)items[i].Amount));
		WriteToDisplay(display);
	}
}
string FormatTypeId(MyInventoryItem item)
{
	switch (item.Type.TypeId.Split('_')[1])
	{
		case "Ore": 			return "Ore " + item.Type.SubtypeId;
		case "SeedItem": 	return "Seed " + item.Type.SubtypeId;
		default: 					return item.Type.SubtypeId;
	}
}
// ------------------------------------------------------------------------------- Production
void CleanProductionInventories()
{
	CleanAssemblers();
	CleanRefinerys();
	CleanAlgaeFarms();
}
void CleanAssemblers()
{
	if (ComponentsInventory == null || MaterialsInventory == null) return;
	for (int i = 0; i < assemblers.Count; i++)
	{
		CleanAssembler(assemblers[i] as IMyAssembler);
	}
}
void CleanAssembler(IMyAssembler assembler)
{
	if (assembler == null) return;
	IMyInventory source, result, sourceStorage, resultStorage;
	if (assembler.Mode == MyAssemblerMode.Assembly)
	{
		source = assembler.InputInventory;
		result = assembler.OutputInventory;
		sourceStorage = MaterialsInventory;
		resultStorage = ComponentsInventory;
	}
	else
	{
		source = assembler.OutputInventory;
		result = assembler.InputInventory;
		sourceStorage = ComponentsInventory;
		resultStorage = MaterialsInventory;
	}

	if (source == null || result == null || sourceStorage == null || resultStorage == null) return;

	if (assembler.IsProducing) MoveOneItem(result, resultStorage);
	else MoveAllItems(result, resultStorage);

	if (!assembler.IsProducing && assembler.IsQueueEmpty) MoveAllItems(source, sourceStorage);
}
void CleanRefinerys()
{
	if (refinerys != null && MaterialsInventory != null)
	{
		for (int i = 0; i < refinerys.Count; i++)
		{
			CleanRefinery(refinerys[i] as IMyRefinery);
		}
	}
}
void CleanRefinery(IMyRefinery refinery)
{
	if (refinery == null) return;
	if (refinery.IsProducing)MoveOneItem(refinery.GetInventory(1), MaterialsInventory);
	else MoveAllItems(refinery.GetInventory(1), MaterialsInventory);

}
void CleanAlgaeFarms()
{
	foreach (var block in algaeFarms)
	{
		CleanAlgaeFarm(block);
	}
}
void CleanAlgaeFarm(IMyFunctionalBlock algaeFarm)
{
	if (algaeFarm == null || ConsumblesInventory == null) return;
	MoveOneItem(algaeFarm.GetInventory(0), ConsumblesInventory);
}
void SortComponents()
{
	items.Clear();
	ComponentsInventory.GetItems(items);

	for (int i = items.Count - 1; i >= 0; i--)
	{
		switch (items[i].Type.TypeId.Split('_')[1])
		{
			case "Ingot":
			case "Ore":
			  MoveOneItem(ComponentsInventory, MaterialsInventory, i);	break;
			case "SeedItem":
			case "ConsumableItem":
			  MoveOneItem(ComponentsInventory, ConsumblesInventory, i);	break;
			case "PhysicalGunObject":
			case "AmmoMagazine":
			  MoveOneItem(ComponentsInventory, AmmoInventory, i);			  break;
		}
	}
}
// ------------------------------------------------------------------------------- Probe
MyDetectedEntityInfo GetCameraTarget(IMyCameraBlock camera, double distance = 5000)
{
	if (!camera.EnableRaycast)
		camera.EnableRaycast = true;
	if (camera.CanScan(distance)) return camera.Raycast((float)distance);
	return new MyDetectedEntityInfo();
}
void AquireTarget(int distance)
{
	_sb.Clear();
	_sb.AppendLine("== Probe ==");
  if (ProbeCamera == null)
  {
    _sb.AppendLine("No camera found: " + ProbeCameraKeyword);
	}
	else {
		MyDetectedEntityInfo info = GetCameraTarget(ProbeCamera, distance);
		if (info.IsEmpty()) 
		{
			_sb.AppendLine("No target in sight");
		}
		else
		{
			_sb.AppendLine(string.Format("Target:   {0}", info.Name));
			_sb.AppendLine(string.Format("Type:     {0}", info.Type));
			_sb.AppendLine(string.Format("Velocity: {0:N2} m/s", info.Velocity.Length()));
			_sb.AppendLine(string.Format("Distance: {0:N2} m", Vector3D.Distance(ProbeCamera.GetPosition(), info.Position)));
			_sb.AppendLine(CreateGPS("Target", info.HitPosition));
		}
				
		_sb.AppendLine(string.Format("scan range: {0} km", ProbeCamera.AvailableScanRange / 1000));
		_sb.AppendLine(string.Format("scan cooldown: {0} s", ProbeCamera.TimeUntilScan(20000) / 1000));
		_sb.AppendLine(string.Format("distance limit: {0}", ProbeCamera.RaycastDistanceLimit));
		_sb.AppendLine(string.Format("time multiplier: {0}", ProbeCamera.RaycastTimeMultiplier));
	}
	for (int i = 0; i < ProbeDisplay.Count; i++)
	{
		WriteToDisplay(GetTextSurface(ProbeDisplay[i],0), true);
	}
}
// ------------------------------------------------------------------------------- Storage Queue
void ProcessAssemblerQueue(IMyTerminalBlock storageBlock)
{
	bool exit = false;
	IMyAssembler target = null;
	string customData;

	if (storageBlock == null) return;
	IMyInventory inventory = storageBlock.GetInventory(0);
	if (inventory == null) return;
	items.Clear();
	inventory.GetItems(items);
	customData = storageBlock.CustomData;
	// --- Fill mode: CustomData is empty, snapshot current inventory as baseline ---
	if (string.IsNullOrEmpty(customData.Trim()))
	{
	_sb.Clear();
	for (int i = 0; i < items.Count; i++)
		_sb.AppendLine(string.Format("{0}: {1}", items[i].Type.SubtypeId, (double)items[i].Amount));
	storageBlock.CustomData = _sb.ToString();
	return;
	}

	if (assemblers.Count == 0) return;
	// --- Assembler check: exit if any assembler is currently producing ---
	for (int i = 0; i < assemblers.Count; i++)
	{
	if(assemblers[i].DefinitionDisplayNameText.Contains("Survival Kit")) continue;
	if(assemblers[i].DefinitionDisplayNameText.Contains("Food Processor")) continue;

	if (target == null && assemblers[i].Mode == MyAssemblerMode.Assembly && !assemblers[i].CooperativeMode) 
	{
		target = assemblers[i];
		if (Me.CustomData == "getasmqueue")
		{
			Me.CustomData = "";
			List<MyProductionItem> items = new List<MyProductionItem>();
			target.GetQueue(items);
			foreach (var item in items)
			{
				Me.CustomData += string.Format("{1}: {0}\n", item.Amount, item.BlueprintId.ToString().Split('/')[1]);
			}
		}
		exit = target.IsProducing || !target.IsQueueEmpty;
	}
	else if (!exit && assemblers[i].CooperativeMode && (assemblers[i].IsProducing || !assemblers[i].IsQueueEmpty))exit = true;
	}
	if (target == null || exit) return;
	// --- Queue mode: parse CustomData as "TypeId/SubtypeId: amount" pairs ---
	var requested = new Dictionary<string, double>();
	string[] lines = customData.Split('\n');
	for (int i = 0; i < lines.Length; i++)
	{
		string trimmed = lines[i].Trim();
		if (string.IsNullOrEmpty(trimmed)) continue;
		int colonIdx = trimmed.IndexOf(':');
		if (colonIdx < 0) continue;
		string key = trimmed.Substring(0, colonIdx).Trim();
		double amount;
		if (!double.TryParse(trimmed.Substring(colonIdx + 1).Trim(), out amount)) continue;
		requested[key] = amount;
	}

	// Build present-amount lookup from current inventory (keyed by item SubtypeId)
	var present = new Dictionary<string, double>();
	for (int i = 0; i < items.Count; i++)
		present[items[i].Type.SubtypeId] = (double)items[i].Amount;
	// --- Queue deficit amounts (requested - present) ---
	foreach (var kvp in requested)
	{
		double have = 0;
		present.TryGetValue(kvp.Key, out have);
		double diff = kvp.Value - have;
		if (diff <= 0) continue;

		// Resolve blueprint name: use map for ammo, fall back to SubtypeId for components
		string blueprintSubtype;
		if (!assemblerBlueprintMap.TryGetValue(kvp.Key, out blueprintSubtype))
			blueprintSubtype = kvp.Key;
		MyDefinitionId blueprintId = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprintSubtype);
		if (target.CanUseBlueprint(blueprintId))
			target.AddQueueItem(blueprintId, (MyFixedPoint)diff);
	}
}
// ------------------------------------------------------------------------------- Solar
void SolarAdjust()
{
	if (solarPanels == null || solarPanels.Count == 0 || solarAzimuthRotor == null) return;
	IMySolarPanel solar = solarPanels[0];



	if (solar != null)
	{
		SolarPowerOutputPrev = SolarPowerOutput;
		SolarPowerOutput = solar.MaxOutput / 0.16;
		double delta = SolarPowerOutput - SolarPowerOutputPrev;
    double maxOutput = solarPanels.Sum(panel => panel.MaxOutput);//solar.MaxOutput * solarPanels.Count;
		double totalOutput = solarPanels.Sum(panel => panel.CurrentOutput);//solar.CurrentOutput * solarPanels.Count;
		
		_sb.Clear();
		_sb.AppendLine("Solar Panels");
		_sb.AppendLine(string.Format("Power:     {0,11:P8}", SolarPowerOutput));
		_sb.AppendLine(string.Format("Delta:     {1}{0,11:P8}", delta, (delta >= 0) ? "+" : ""));
		_sb.AppendLine(string.Format("Generated: {0,7:N3} kW", maxOutput * 1000));
		_sb.AppendLine(string.Format("Used:      {0,7:N3} kW", totalOutput * 1000));

		if (solarCTC == null)
		{		
			SolarConfigureAxisAligner();
			if (delta < 0 && Math.Abs(delta) > 3.0 * Proximity)
			{
				SolarRedirects++;
				SolarDirectionSwitch = SolarDirectionSwitch * (-1.0 / SolarRedirects);
			}
			else
			{
				SolarDirectionSwitch = Math.Sign(SolarDirectionSwitch);
				SolarRedirects = 1;
			}
			_sb.AppendLine(string.Format("Redirect:  {0}", SolarRedirects));
			_sb.AppendLine(string.Format("Switch:    {0}", SolarDirectionSwitch));
			if (solarAzimuthRotor != null)
			{
				solarAzimuthRotor.BrakingTorque = solarAzimuthRotor.Torque;

				if ((1.0 - Proximity) > SolarPowerOutput && SolarPowerOutput != 0)
				{
					solarAzimuthRotor.TargetVelocityRPM = (float)(2 * SolarDirectionSwitch * (1 - SolarPowerOutput));
				}
				else
				{
					solarAzimuthRotor.TargetVelocityRPM = 0;
					SolarDirectionSwitch = Math.Sign(SolarDirectionSwitch);
				}

				_sb.AppendLine(string.Format("Angle:     {0:F8} rad", solarAzimuthRotor.Angle));
				_sb.AppendLine(string.Format("Velocity:  {0:F8} RPM", solarAzimuthRotor.TargetVelocityRPM));
			}
		}
		else
		{
			SolarConfigureCTC();
		}
		if (solarDisplay != null)
			WriteToDisplay(solarDisplay);
		else
			Echo(_sb.ToString());
	}
}
void SolarConfigureCTC()
{
	_sb.AppendLine("Solar CTC found");
	solarCTC.Camera = solarCamera;
	_sb.AppendLine("Solar Tracker camera set");
	solarCTC.AzimuthRotor = solarAzimuthRotor;
	_sb.AppendLine("Solar Azimuth rotor set");
	solarCTC.IsSunTrackerEnabled = true;
	_sb.AppendLine("Sun tracking enabled");
}
void SolarConfigureAxisAligner()
{
	if (solarController != null)
	{
		Vector3D pos = solarController.GetPosition();
		solarController.ClearWaypoints();
		solarController.ControlThrusters = false;
		solarController.FlightMode = FlightMode.OneWay;
		solarController.AddWaypoint(new Vector3D(pos.X, pos.Y + 1000000000, pos.Z), "SOL");
	}
}
// ------------------------------------------------------------------------------- util
void assert(bool cond, String errormsg)
{
	if (!cond) throw new Exception(errormsg);
}
void MoveOneItem(IMyInventory source, IMyInventory dest, int index = 0)
{
	if (source == null || dest == null) return;
	items.Clear();
	source.GetItems(items);
	if (items.Count > 0)
		source.TransferItemTo(dest, index, null, true, null);
}
void MoveAllItems(IMyInventory source, IMyInventory dest)
{
	if (source == null || dest == null) return;
	items.Clear();
	source.GetItems(items);

	for (int i = 0; i < items.Count; i++)
	{
		source.TransferItemTo(dest, 0, null, true, null);
	}
}
void MoveGridContentsToInventory(string gridNameSelector, IMyInventory target)
{
	//MoveGridContentsToInventory("Miner", MaterialsInventory); 
	if (target == null) return;
	var gridBlocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(gridBlocks,
		block => block.CubeGrid.CustomName.Contains(gridNameSelector));
	for (int i = 0; i < gridBlocks.Count; i++)
	{
		for (int j = 0; j < gridBlocks[i].InventoryCount; j++)
		{
			IMyInventory source = gridBlocks[i].GetInventory(j);
			if (source != null && source != target)
				MoveAllItems(source, target);
		}
	}
}
void BlueprintMapFill()
{
	// Assembler: item SubtypeId -> blueprint SubtypeId
	assemblerBlueprintMap["Missile200mm"]                        = "Position0100_Missile200mm";
	assemblerBlueprintMap["AutocannonClip"]                      = "Position0090_AutocannonClip";
	assemblerBlueprintMap["SmallRailgunAmmo"]                    = "Position0130_SmallRailgunAmmo";
	assemblerBlueprintMap["LargeRailgunAmmo"]                    = "Position0140_LargeRailgunAmmo";
	assemblerBlueprintMap["LargeCalibreAmmo"]                    = "Position0120_LargeCalibreAmmo";
	assemblerBlueprintMap["MediumCalibreAmmo"]                   = "Position0110_MediumCalibreAmmo";
	assemblerBlueprintMap["ElitePistolMagazine"]                 = "Position0030_ElitePistolMagazine";
	assemblerBlueprintMap["NATO_25x184mm"]                       = "Position0080_NATO_25x184mmMagazine";
	assemblerBlueprintMap["SemiAutoPistolMagazine"]              = "Position0010_SemiAutoPistolMagazine";
	assemblerBlueprintMap["FullAutoPistolMagazine"]              = "Position0020_FullAutoPistolMagazine";
	assemblerBlueprintMap["AutomaticRifleGun_Mag_20rd"]          = "Position0040_AutomaticRifleGun_Mag_20rd";
	assemblerBlueprintMap["PreciseAutomaticRifleGun_Mag_5rd"]    = "Position0060_PreciseAutomaticRifleGun_Mag_5rd";
	assemblerBlueprintMap["UltimateAutomaticRifleGun_Mag_30rd"]  = "Position0070_UltimateAutomaticRifleGun_Mag_30rd";
	assemblerBlueprintMap["RapidFireAutomaticRifleGun_Mag_50rd"] = "Position0050_RapidFireAutomaticRifleGun_Mag_50rd";
	// components 
	assemblerBlueprintMap["Motor"]                = "MotorComponent";
	assemblerBlueprintMap["Girder"]               = "GirderComponent";
	assemblerBlueprintMap["Thrust"]               = "ThrustComponent";
	assemblerBlueprintMap["Medical"]              = "MedicalComponent";
	assemblerBlueprintMap["Reactor"]              = "ReactorComponent";
	assemblerBlueprintMap["Computer"]             = "ComputerComponent";
	assemblerBlueprintMap["Detector"]             = "DetectorComponent";
	assemblerBlueprintMap["Explosives"]           = "ExplosivesComponent";
	assemblerBlueprintMap["Construction"]         = "ConstructionComponent";
	assemblerBlueprintMap["GravityGenerator"]     = "GravityGeneratorComponent";
	assemblerBlueprintMap["RadioCommunication"]   = "RadioCommunicationComponent";


	// assemblerBlueprintMap["Display"]              = "Display";
	// assemblerBlueprintMap["PowerCell"]            = "PowerCell";
	// assemblerBlueprintMap["LargeTube"]            = "LargeTube";
	// assemblerBlueprintMap["SmallTube"]            = "SmallTube";
	// assemblerBlueprintMap["MetalGrid"]            = "MetalGrid";
	// assemblerBlueprintMap["SolarCell"]            = "SolarCell";
	// assemblerBlueprintMap["SteelPlate"]           = "SteelPlate";
	// assemblerBlueprintMap["InteriorPlate"]        = "InteriorPlate";
	// assemblerBlueprintMap["Superconductor"]       = "Superconductor";
	// assemblerBlueprintMap["BulletproofGlass"]     = "BulletproofGlass";
}
void FormatAllDisplays(IMyTextSurfaceProvider DisplayBlock)
{
	for (int i = 0; i < DisplayBlock.SurfaceCount; i++)
	{
		var surface = GetTextSurface(DisplayBlock, i);
		if (surface != null)
		{
			surface.ContentType = ContentType.TEXT_AND_IMAGE;
			surface.Font = font;
			surface.FontColor = foregroundColor;
			surface.BackgroundColor = backgroundColor;

			surface.ScriptBackgroundColor = backgroundColor;
			surface.ScriptForegroundColor = foregroundColor;
		}
	}
}
void WriteToDisplay(IMyTextSurface surface = null, bool echoToo = false)
{
  if (surface != null)
  {
    surface.Font = font;
    surface.FontColor = foregroundColor;
    surface.BackgroundColor = backgroundColor;
    surface.WriteText(_sb);
  }
  else
  {
    Me.CustomData = _sb.ToString();
  }
  if (echoToo)Echo(_sb.ToString());
}
void SetName(IMyTerminalBlock block, string name)
{
	if (block != null)
	{
		block.CustomName = name;
	}
}
bool BlockCountChanged()
{
	GridTerminalSystem.GetBlocks(L);
	CurrentBlockCount = L.Count;
	bool changed = CurrentBlockCount != PastBlockCount;
	PastBlockCount = CurrentBlockCount;
	return changed;
}
string 		CreateGPS(string name, Vector3D? pos)
{
	if (pos == null) return $"GPS:{name}:::::#FF00FF00:";
	return $"GPS:{name}:{pos.Value.X:F2}:{pos.Value.Y:F2}:{pos.Value.Z:F2}:#FF00FF00:";
}
double[] 	GetXYZ(IMyTerminalBlock block)
{
	return new double[] { block.GetPosition().GetDim(0), block.GetPosition().GetDim(1), block.GetPosition().GetDim(2) };
}
IMyTextSurface 					GetTextSurface(IMyTextSurfaceProvider DisplayBlock, int panel = 0)
{
	return (DisplayBlock != null) ? DisplayBlock.GetSurface(panel) : null;
}
IMyTerminalBlock				BlockNamed(String str)
{
	L.Clear();
	L = BlocksNamed(str);
	return L.Count > 0 ? L[0] : null;
}
List<IMyTerminalBlock> 	BlocksNamed(String str)
{
	L.Clear();
	GridTerminalSystem.SearchBlocksOfName(str, L);
	return L;
}
