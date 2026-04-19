// SEMF - Space Engineers Management Framework
// https://malforge.github.io/spaceengineers/pbapi/
int LightRadius;

string StorageMaterialsKeyword;
string StorageComponentsKeyword;
string StorageMaterialsDisplayKeyword;
string StorageComponentsDisplayKeyword;
int StorageMaterialsDisplayPanel;
int StorageComponentsDisplayPanel;

string AlgaeFarmKeyword;

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
List<IMyTerminalBlock> L = new List<IMyTerminalBlock>();

List<IMyReflectorLight> ReflectorLightBlocks = new List<IMyReflectorLight>();
List<IMyInteriorLight> InteriorLightBlocks = new List<IMyInteriorLight>();
List<IMyLightingBlock> interiorLightsCast = new List<IMyLightingBlock>();
List<IMyLightingBlock> reflectorLightsCast = new List<IMyLightingBlock>();

List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyRefinery> refinerys = new List<IMyRefinery>();

List<IMyMedicalRoom> RefillPoints = new List<IMyMedicalRoom>();

List<MyInventoryItem> items = new List<MyInventoryItem>();
List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
// List<IMySolarFoodGenerator> algaeFarms = new List<IMySolarFoodGenerator>();
List<IMyTerminalBlock> algaeFarms = new List<IMyTerminalBlock>();
////////////////////////////////////// LISTS ///////////////////////////////////

Color interiorLightColor;
Color reflectorLightColor;
Color foregroundColor;
Color backgroundColor;

////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////
IMyTerminalBlock invMatBlock;
IMyTerminalBlock invCompBlock;
IMyMotorStator solarAzimuthRotor;
IMyRemoteControl solarController;
IMyCameraBlock solarCamera;
IMyTextSurface solarDisplay;
IMyTurretControlBlock solarCTC;
IMyTextSurface materialsDisplay;
IMyTextSurface componentsDisplay;

System.Text.StringBuilder _sb = new System.Text.StringBuilder();
////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////

int Ticker;
const int TickerLimit = 6;
double[] tickTimes = new double[TickerLimit];
double[] tickMaxTimes = new double[TickerLimit];
double[] tickInstructions = new double[TickerLimit];
DateTime tickStart;
public Program()
{
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
	StorageMaterialsDisplayPanel = 1;
	
	StorageComponentsKeyword = "[Components]";
	StorageComponentsDisplayKeyword = "[CompDisplay]";
	StorageComponentsDisplayPanel = 0;
	// Algae -----------------------------------------------------------------------
	AlgaeFarmKeyword = "Algae Farm";
	// Solar -----------------------------------------------------------------------
	SolarPointerRCKeyword = "[Axis Aligner]";
	solarAzimuthRotorKeyword = "[Solar Wing]";
	SolarDisplayKeyword = "[SolarDisplay]";
	SolarCTCKeyword = "[SolarCTC]";
	SolarCameraKeyword = "[SolarTracker]";

	SolarDisplaypanel = 2;
	SolarDirectionSwitch = 1;
	SolarRedirects = 0;
	Proximity = 0.001;
}

//public void Save(){}

public void Main(string argument, UpdateType updateSource)
{
	if ((updateSource & UpdateType.Update10) == 0) return;

	EchoStats();

	tickStart = DateTime.Now;
	switch (Ticker)
	{
		case 0: RefreshBlockCache(); break;
		case 1: InteriorLightAdjust(); break;
		case 2: ReflectorLightAdjust(); break;
		case 3: SolarAdjust(); DisplayClocks(); break;
		case 4: CleanProductionInventories(); break;
		case 5: DisplayStorageContents(); break;
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
	_sb.AppendLine("[T] Function  ms(max) | instr");
	_sb.AppendLine(string.Format("[0] Cache:    {0:F0} ({1,3:F0}) | {2}", tickTimes[0], tickMaxTimes[0], tickInstructions[0]));
	_sb.AppendLine(string.Format("[1] IntLight: {0:F0} ({1,3:F0}) | {2}", tickTimes[1], tickMaxTimes[1], tickInstructions[1]));
	_sb.AppendLine(string.Format("[2] RefLight: {0:F0} ({1,3:F0}) | {2}", tickTimes[2], tickMaxTimes[2], tickInstructions[2]));
	_sb.AppendLine(string.Format("[3] Solar:    {0:F0} ({1,3:F0}) | {2}", tickTimes[3], tickMaxTimes[3], tickInstructions[3]));
	_sb.AppendLine(string.Format("[4] ProdClean:{0:F0} ({1,3:F0}) | {2}", tickTimes[4], tickMaxTimes[4], tickInstructions[4]));
	_sb.AppendLine(string.Format("[5] Storage:  {0:F0} ({1,3:F0}) | {2}", tickTimes[5], tickMaxTimes[5], tickInstructions[5]));
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
	
	//GridTerminalSystem.GetBlocksOfType<IMySolarFoodGenerator>(algaeFarms);
	GridTerminalSystem.SearchBlocksOfName(AlgaeFarmKeyword, algaeFarms);

	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(InteriorLightBlocks);
	GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(ReflectorLightBlocks);

	interiorLightsCast.Clear();
	for (int i = 0; i < InteriorLightBlocks.Count; i++) interiorLightsCast.Add(InteriorLightBlocks[i]);
	reflectorLightsCast.Clear();
	for (int i = 0; i < ReflectorLightBlocks.Count; i++) reflectorLightsCast.Add(ReflectorLightBlocks[i]);

	invMatBlock        = BlockNamed(StorageMaterialsKeyword);
	invCompBlock       = BlockNamed(StorageComponentsKeyword);
	solarAzimuthRotor  = BlockNamed(solarAzimuthRotorKeyword) as IMyMotorStator;
	solarController    = BlockNamed(SolarPointerRCKeyword) as IMyRemoteControl;
	solarCTC           = BlockNamed(SolarCTCKeyword) as IMyTurretControlBlock;
	solarCamera        = BlockNamed(SolarCameraKeyword) as IMyCameraBlock;

	solarDisplay       = GetTextSurface(BlockNamed(SolarDisplayKeyword) as IMyTextSurfaceProvider, SolarDisplaypanel);
	materialsDisplay   = GetTextSurface(BlockNamed(StorageMaterialsDisplayKeyword) as IMyTextSurfaceProvider, StorageMaterialsDisplayPanel);
	componentsDisplay  = GetTextSurface(BlockNamed(StorageComponentsDisplayKeyword) as IMyTextSurfaceProvider, StorageComponentsDisplayPanel);
}
IMyTextSurface GetTextSurface(IMyTextSurfaceProvider DisplayBlock, int panel = 0)
{
	return (DisplayBlock != null) ? DisplayBlock.GetSurface(panel) : null;
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
void LightAdjust(List<IMyLightingBlock> LightBlocks, Color color, int LightRadius = 10, int intensity = 5)
{
	for (int i = 0; i < LightBlocks.Count; i++)
	{
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
}
void DisplayClock(IMyTextSurfaceProvider DisplayBlock, int panel = 0)
{
	var surface = GetTextSurface(DisplayBlock, panel);
	if (surface != null)
	{
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
}
void DisplayStoredComponents()
{
	if (invCompBlock == null) return;
	DisplayStored(invCompBlock.GetInventory(0), componentsDisplay, "== Components ==");
}
void DisplayStoredMaterials()
{
	if (invMatBlock == null) return;
	DisplayStored(invMatBlock.GetInventory(0), materialsDisplay, "== Materials ==", "{0,-15}: {1,13:N2}");
}
void DisplayStored(IMyInventory inventory, IMyTextSurface display, string title, string format = "{0,-20}: {1,6:N0}")
{
	if (inventory != null && display != null)
	{
		items.Clear();
		_sb.Clear();
		_sb.AppendLine(title);
		inventory.GetItems(items);
		items.Sort((a, b) => ((double)b.Amount).CompareTo((double)a.Amount));
		for (int i = 0; i < items.Count; i++)
			_sb.AppendLine(string.Format(format, items[i].Type.TypeId.Contains("Ore") ? items[i].Type.SubtypeId + " Ore" : items[i].Type.SubtypeId, (double)items[i].Amount));
		display.WriteText(_sb);
	}
}
// ------------------------------------------------------------------------------- Production
void CleanProductionInventories()
{
	IMyInventory MaterialsInventory = (invMatBlock != null) ? invMatBlock.GetInventory(0) : null;
	IMyInventory ComponentsInventory = (invCompBlock != null) ? invCompBlock.GetInventory(0) : null;

	CleanAssemblers(ComponentsInventory, MaterialsInventory);
	CleanRefinerys(MaterialsInventory);
	CollectAlgaeFarm(MaterialsInventory);
}
void CleanAssemblers(IMyInventory ComponentsInventory, IMyInventory MaterialsInventory)
{
	for (int i = 0; i < assemblers.Count; i++)
	{
		CleanAssembler(assemblers[i] as IMyAssembler, ComponentsInventory, MaterialsInventory);
	}
}
void CleanAssembler(IMyAssembler assembler, IMyInventory ComponentsInventory, IMyInventory MaterialsInventory)
{
	if (assembler != null)
	{
		IMyInventory source, result, sourceStorage, resultStorage;
		if (assembler.Mode == MyAssemblerMode.Assembly)
		{
			source = assembler.GetInventory(0);
			sourceStorage = MaterialsInventory;

			result = assembler.GetInventory(1);
			resultStorage = ComponentsInventory;
		}
		else
		{
			source = assembler.GetInventory(1);
			sourceStorage = ComponentsInventory;

			result = assembler.GetInventory(0);
			resultStorage = MaterialsInventory;
		}


		if (resultStorage != null)
		{
			MoveOneItem(result, resultStorage);
		}

		if (!assembler.IsProducing && sourceStorage != null)
		{
			MoveOneItem(source, sourceStorage);
		}
	}
}
void CleanRefinerys(IMyInventory MaterialsInventory)
{
	if (refinerys != null && MaterialsInventory != null)
	{
		for (int i = 0; i < refinerys.Count; i++)
		{
			CleanRefinery(refinerys[i] as IMyRefinery, MaterialsInventory);
		}
	}
}
void CleanRefinery(IMyRefinery refinery, IMyInventory MaterialsInventory)
{
	if (refinery != null)
	{
		// MoveAllItems(refinery.GetInventory(1), MaterialsInventory);
		MoveOneItem(refinery.GetInventory(1), MaterialsInventory);
	}
}
// ------------------------------------------------------------------------------- Algae
void CollectAlgaeFarm(IMyInventory targetInventory)
{
	foreach (var block in algaeFarms)
	{
		var inv = block.GetInventory(0);
		if (inv != null && targetInventory != null)
		{
			MoveOneItem(inv, targetInventory);
		}
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
    double maxOutput = solar.MaxOutput * solarPanels.Count;
		double totalOutput = solar.CurrentOutput * solarPanels.Count;

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
			solarDisplay.WriteText(_sb);
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
void MoveAllItems(IMyInventory source, IMyInventory dest)
{
	if (source == null || dest == null) return;
	items.Clear();
	source.GetItems(items);

	for (int i = items.Count - 1; i >= 0; i--)
	{
		source.TransferItemTo(dest, i, null, true, null);
	}
}
void MoveOneItem(IMyInventory source, IMyInventory dest)
{
	if (source == null || dest == null) return;
	items.Clear();
	source.GetItems(items);
	if (items.Count > 0)
		source.TransferItemTo(dest, 0, null, true, null);
}
double[] GetXYZ(IMyTerminalBlock block)
{
	return new double[] { block.GetPosition().GetDim(0), block.GetPosition().GetDim(1), block.GetPosition().GetDim(2) };
}
void SetName(IMyTerminalBlock block, string name)
{
	if (block != null)
	{
		block.CustomName = name;
	}
}
List<IMyTerminalBlock> BlocksNamed(String str)
{
	L.Clear();
	GridTerminalSystem.SearchBlocksOfName(str, L);
	return L;
}
IMyTerminalBlock BlockNamed(String str)
{
	L.Clear();
	L = BlocksNamed(str);
	return L.Count > 0 ? L[0] : null;
}
void assert(bool cond, String errormsg)
{
	if (!cond) throw new Exception(errormsg);
}