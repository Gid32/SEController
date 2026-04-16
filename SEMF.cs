// SEMF - Space Engineers Management Framework
// https://malforge.github.io/spaceengineers/pbapi/

int cR, cG, cB;
int LightRadius;

string StorageMaterialsKeyword;
string StorageComponentsKeyword;
string AlgaeFarmKeyword;

string SolarPointerRCKeyword;
string SolarRotorKeyword;
string SolarPanelKeyword;
string SolarDisplayKeyword;
int SolarRedirects;
double Proximity, SolarDirectionSwitch;
double SolarPowerOutput, SolarPowerOutputPrev;

////////////////////////////////////// LISTS ///////////////////////////////////
List<IMyReflectorLight> ReflectorLightBlocks = new List<IMyReflectorLight>();
List<IMyInteriorLight> InteriorLightBlocks = new List<IMyInteriorLight>();

List<IMyAssembler> assemblers = new List<IMyAssembler>();
List<IMyRefinery> refinerys = new List<IMyRefinery>();

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
////////////////////////////////////// CACHED BLOCKS ///////////////////////////////////

int Ticker;
const int TickerLimit = 6;
double[] tickTimes = new double[TickerLimit];
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
	StorageComponentsKeyword = "[Components]";
	// Algae -----------------------------------------------------------------------
	AlgaeFarmKeyword = "Algae Farm";
	// Solar -----------------------------------------------------------------------
	SolarPointerRCKeyword = "[Axis Aligner]";
	SolarRotorKeyword = "[Solar Wing]";
	SolarPanelKeyword = "Solar Panel";
	SolarDisplayKeyword = "[SolarDisplay]";
	SolarDirectionSwitch = 1;
	SolarRedirects = 0;
	Proximity = 0.001;
}

//public void Save(){}

public void Main(string argument, UpdateType updateSource)
{
	if ((updateSource & UpdateType.Update10) == 0) return;

	Echo("Tick times (ms):");
	Echo("  [0] Cache:   " + tickTimes[0].ToString("F2"));
	Echo("  [1] IntLight:" + tickTimes[1].ToString("F2"));
	Echo("  [2] RefLight:" + tickTimes[2].ToString("F2"));
	Echo("  [3] Storage: " + tickTimes[3].ToString("F2"));
	Echo("  [4] Solar:   " + tickTimes[4].ToString("F2"));

	tickStart = DateTime.Now;
	switch (Ticker)
	{
		case 0: RefreshBlockCache(); break;
		case 1: InteriorLightAdjust(); break;
		case 2: ReflectorLightAdjust(); break;
		case 3: CleanProductionInventories(); break;
		case 4: SolarAdjust(); break;
	}
	tickTimes[Ticker] = (DateTime.Now - tickStart).TotalMilliseconds;

	Ticker = (Ticker + 1) % TickerLimit;
}

// ------------------------------------------------------------------------------- Cache

void RefreshBlockCache()
{
	GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>(InteriorLightBlocks);
	GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(ReflectorLightBlocks);
	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refinerys);
	GridTerminalSystem.SearchBlocksOfName(AlgaeFarmKeyword, algaeFarms);
	invMatBlock  = BlockNamed(StorageMaterialsKeyword);
	invCompBlock = BlockNamed(StorageComponentsKeyword);
	solarPanel      = BlockNamed(SolarPanelKeyword) as IMySolarPanel;
	solarRotor      = BlockNamed(SolarRotorKeyword) as IMyMotorStator;
	solarController = BlockNamed(SolarPointerRCKeyword) as IMyRemoteControl;
	var solarDisplayBlock = BlockNamed(SolarDisplayKeyword) as IMyTextSurfaceProvider;
	solarDisplay = (solarDisplayBlock != null) ? solarDisplayBlock.GetSurface(0) : null;
}

// ------------------------------------------------------------------------------- Light

void InteriorLightAdjust()
{
	LightAdjust(InteriorLightBlocks.Cast<IMyLightingBlock>().ToList(), cR, cG, cB, LightRadius);
}

void ReflectorLightAdjust()
{
	LightAdjust(ReflectorLightBlocks.Cast<IMyLightingBlock>().ToList(), 255, 255, 255, 160);
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
			MoveAllItems(result, resultStorage);
		}

		if (!assembler.IsProducing && sourceStorage != null)
		{
			MoveAllItems(source, sourceStorage);
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
			MoveAllItems(inv, targetInventory);
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

		var sb = new System.Text.StringBuilder();
		sb.AppendLine("Solar Panels");
		sb.AppendLine("Power: " + (SolarPowerOutput * 100) + "%");
		sb.AppendLine("Delta: " + (delta * 100) + "%");
		sb.AppendLine("Power Gen.: " + (solar.MaxOutput * 1000) + " kW");
		sb.AppendLine("Power Use.: " + (solar.CurrentOutput * 1000) + " kW");

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
		sb.AppendLine("Redirect: " + SolarRedirects);
		sb.AppendLine("Switch: " + SolarDirectionSwitch);
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

			sb.AppendLine("Angle: " + rotor.Angle + " rad.");
			sb.AppendLine("Velocity: " + rotor.TargetVelocityRPM + " RPM");
		}

		string solarText = sb.ToString();
		if (solarDisplay != null)
			solarDisplay.WriteText(solarText);
		else
			Echo(solarText);
	}
}

void SolarConfigureAxisAligner()
{
	double[] xyz;
	IMyRemoteControl controller = solarController;

	if (controller != null)
	{
		xyz = GetXYZ(controller);
		controller.ClearWaypoints();
		controller.ControlThrusters = false;
		controller.FlightMode = FlightMode.OneWay;
		controller.AddWaypoint(new Vector3D(xyz[0], xyz[1] + 1000000000, xyz[2]), "SOL");
	}
}

// ------------------------------------------------------------------------------- util

void MoveAllItems(IMyInventory source, IMyInventory dest)
{
	source.GetItems(items);

	for (int i = items.Count - 1; i >= 0; i--)
	{
		source.TransferItemTo(dest, i, null, true, null);
	}
}

void MoveOneItem(IMyInventory source, IMyInventory dest)
{
	source.GetItems(items);
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