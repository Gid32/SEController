// SEMF - Space Engineers Management Framework
// https://malforge.github.io/spaceengineers/pbapi/

int cR, cG, cB;
int LightRadius;

string StorageMaterialsKeyword;
string StorageComponentsKeyword;
string StorageMaterialsDisplayKeyword;
string StorageComponentsDisplayKeyword;
int StorageMaterialsDisplayPanel;
int StorageComponentsDisplayPanel;
string AlgaeFarmKeyword;

string SolarPointerRCKeyword;
string SolarRotorKeyword;
string SolarPanelKeyword;
string SolarDisplayKeyword;
int SolarDisplaypanel;
int SolarRedirects;
double Proximity, SolarDirectionSwitch;
double SolarPowerOutput, SolarPowerOutputPrev;

////////////////////////////////////// LISTS ///////////////////////////////////
List<IMyReflectorLight> ReflectorLightBlocks = new List<IMyReflectorLight>();
List<IMyInteriorLight> InteriorLightBlocks = new List<IMyInteriorLight>();

List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyRefinery> refinerys = new List<IMyRefinery>();

List<IMyLightingBlock> interiorLightsCast = new List<IMyLightingBlock>();
List<IMyLightingBlock> reflectorLightsCast = new List<IMyLightingBlock>();
List<MyInventoryItem> items = new List<MyInventoryItem>();
List<IMyTerminalBlock> L = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> algaeFarms = new List<IMyTerminalBlock>();
////////////////////////////////////// LISTS ///////////////////////////////////

////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////
IMyTerminalBlock invMatBlock;
IMyTerminalBlock invCompBlock;
IMySolarPanel solarPanel;
IMyMotorStator solarRotor;
IMyRemoteControl solarController;
IMyTextSurface solarDisplay;
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
	cR = 204;
	cG = 255;
	cB = 140;
	LightRadius = 10;
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
	SolarRotorKeyword = "[Solar Wing]";
	SolarPanelKeyword = "Solar Panel";
	SolarDisplayKeyword = "[SolarDisplay]";
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
		case 3: SolarAdjust(); break;
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
	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(InteriorLightBlocks);
	interiorLightsCast.Clear();
	for (int i = 0; i < InteriorLightBlocks.Count; i++) interiorLightsCast.Add(InteriorLightBlocks[i]);

	GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(ReflectorLightBlocks);
	reflectorLightsCast.Clear();
	for (int i = 0; i < ReflectorLightBlocks.Count; i++) reflectorLightsCast.Add(ReflectorLightBlocks[i]);

	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refinerys);
	GridTerminalSystem.SearchBlocksOfName(AlgaeFarmKeyword, algaeFarms);
	invMatBlock  = BlockNamed(StorageMaterialsKeyword);
	invCompBlock = BlockNamed(StorageComponentsKeyword);
	solarPanel      = BlockNamed(SolarPanelKeyword) as IMySolarPanel;
	solarRotor      = BlockNamed(SolarRotorKeyword) as IMyMotorStator;
	solarController = BlockNamed(SolarPointerRCKeyword) as IMyRemoteControl;
	var solarDisplayBlock = BlockNamed(SolarDisplayKeyword) as IMyTextSurfaceProvider;
	solarDisplay = (solarDisplayBlock != null) ? solarDisplayBlock.GetSurface(SolarDisplaypanel) : null;
	var matDisplayBlock = BlockNamed(StorageMaterialsDisplayKeyword) as IMyTextSurfaceProvider;
	materialsDisplay = (matDisplayBlock != null) ? matDisplayBlock.GetSurface(StorageMaterialsDisplayPanel) : null;
	var compDisplayBlock = BlockNamed(StorageComponentsDisplayKeyword) as IMyTextSurfaceProvider;
	componentsDisplay = (compDisplayBlock != null) ? compDisplayBlock.GetSurface(StorageComponentsDisplayPanel) : null;
}

// ------------------------------------------------------------------------------- Light

void InteriorLightAdjust()
{
	LightAdjust(interiorLightsCast, cR, cG, cB, LightRadius);
}

void ReflectorLightAdjust()
{
	LightAdjust(reflectorLightsCast, 255, 255, 255, 160);
}

void LightAdjust(List<IMyLightingBlock> LightBlocks, int R = 255, int G = 255, int B = 255, int LightRadius = 10, int intensity = 5)
{
	var color = new Color(R, G, B, 255);
	for (int i = 0; i < LightBlocks.Count; i++)
	{
		LightBlocks[i].Radius = LightRadius;
		LightBlocks[i].Intensity = intensity;
		LightBlocks[i].Color = color;
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
	DisplayStored(invCompBlock.GetInventory(0), componentsDisplay, "== Components ==");
}

void DisplayStoredMaterials()
{
	DisplayStored(invMatBlock.GetInventory(0), materialsDisplay, "== Materials ==", "{0,-10}{2:3}: {1,15:N2}");
}

void DisplayStored(IMyInventory inventory, IMyTextSurface display, string title, string format = "{0,-20}: {1,6:N0}")
{
	if (inventory != null && display != null)
	{
		items.Clear();
		_sb.Clear();
		_sb.AppendLine(title);
		inventory.GetItems(items);
		for (int i = 0; i < items.Count; i++)
			_sb.AppendLine(string.Format(format, items[i].Type.SubtypeId, (double)items[i].Amount, items[i].Type.TypeId.Contains("Ore") ? "Ore" : "   "));
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
	IMySolarPanel solar = solarPanel;
	IMyMotorStator rotor = solarRotor;

	SolarConfigureAxisAligner();

	if (solar != null)
	{
		SolarPowerOutputPrev = SolarPowerOutput;
		SolarPowerOutput = solar.MaxOutput / 0.16;
		double delta = SolarPowerOutput - SolarPowerOutputPrev;

		_sb.Clear();
		_sb.AppendLine("Solar Panels");
		_sb.AppendLine(string.Format("Power:     {0,11:P8}", SolarPowerOutput));
		_sb.AppendLine(string.Format("Delta:     {1}{0,11:P8}", delta, (delta >= 0) ? "+" : ""));
		_sb.AppendLine(string.Format("Generated: {0,7:F3} kW", solar.MaxOutput * 1000));
		_sb.AppendLine(string.Format("Used:      {0,7:F3} kW", solar.CurrentOutput * 1000));

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
		if (rotor != null)
		{
			rotor.BrakingTorque = rotor.Torque;

			if ((1.0 - Proximity) > SolarPowerOutput && SolarPowerOutput != 0)
			{
				rotor.TargetVelocityRPM = (float)(2 * SolarDirectionSwitch * (1 - SolarPowerOutput));
			}
			else
			{
				rotor.TargetVelocityRPM = 0;
				SolarDirectionSwitch = Math.Sign(SolarDirectionSwitch);
			}

			_sb.AppendLine(string.Format("Angle:     {0:F8} rad", rotor.Angle));
			_sb.AppendLine(string.Format("Velocity:  {0:F8} RPM", rotor.TargetVelocityRPM));
		}

		if (solarDisplay != null)
			solarDisplay.WriteText(_sb);
		else
			Echo(_sb.ToString());
	}
}

void SolarConfigureAxisAligner()
{
	IMyRemoteControl controller = solarController;

	if (controller != null)
	{
		Vector3D pos = controller.GetPosition();
		controller.ClearWaypoints();
		controller.ControlThrusters = false;
		controller.FlightMode = FlightMode.OneWay;
		controller.AddWaypoint(new Vector3D(pos.X, pos.Y + 1000000000, pos.Z), "SOL");
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
	GridTerminalSystem.SearchBlocksOfName(str, L);
	return L;
}

IMyTerminalBlock BlockNamed(String str)
{
	List<IMyTerminalBlock> L = BlocksNamed(str);
	return L.Count > 0 ? L[0] : null;
}

void assert(bool cond, String errormsg)
{
	if (!cond) throw new Exception(errormsg);
}