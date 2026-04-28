string ScopeCameraKeyword;
int    ScopeDisplayPanel;

List<IMyTextSurfaceProvider> ScopeDisplay = new List<IMyTextSurfaceProvider>();
IMyCameraBlock ScopeCamera;

const string PREFIX       = "TSH";

// -- Cached Blocks --------------------------------------------------------------
IMyShipConnector         _connector;
IMyRadioAntenna          _antenna;
IMyOreDetector           _oreDetector;
IMyBatteryBlock          _battery;
IMyShipController        _cockpit;
IMyFlightMovementBlock   _aiMove;   
IMyPathRecorderBlock     _aiRec;    

List<IMyGyro>   _gyros     = new List<IMyGyro>();
List<IMyThrust> _thrusters = new List<IMyThrust>();

// -- Shared ---------------------------------------------------------------------
List<IMyTerminalBlock>    L   = new List<IMyTerminalBlock>();
System.Text.StringBuilder _sb = new System.Text.StringBuilder();

Color foregroundColor = new Color(100, 255, 100, 255);
Color backgroundColor = new Color(0, 0, 0, 255);
const string font = "Monospace";

// -- Lifecycle ------------------------------------------------------------------
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.None;
    ScopeCameraKeyword = "[TSHScopeCamera]";
    ScopeDisplayPanel = 1;
    RefreshBlockCache();
    FormatAllDisplays(Me);
    FormatAllDisplays(_cockpit as IMyTextSurfaceProvider, false);
}

public void Main(string argument, UpdateType updateSource)
{
    if (StringContains(argument, "refresh") || !BlocksIntact()) {RefreshBlockCache(); return;}
    if (StringContains(argument, "homein"))  { DockingSequence(); return; }
    if (StringContains(argument, "undock")) {Undock(); return;}
    if (StringContains(argument, "dock")) {Dock(); return;}
    
    if (argument.IndexOf("navscan", StringComparison.OrdinalIgnoreCase) >= 0)
    {
        int distance = 20000;
        if (argument.Trim().Split(' ').Length >= 2) int.TryParse(argument.Trim().Split(' ')[1], out distance);

        AquireTarget(distance);
    }
}

// -- Docking --------------------------------------------------------------------
void Undock()
{
  if (BlocksIntact())
  {
    Echo("Undocking...");
    _connector.Disconnect();
    if (!_connector.IsConnected) { 
      SetCollisionAvoidance(true);
      SetPrecisionMode(true);
      SetThrusters(true);
      SetGyros(true);
      _aiRec.Enabled = false;
      _aiMove.Enabled = false;
      _gyros.ForEach(g => g.Enabled = true);
      _thrusters.ForEach(t => t.Enabled = true);
      _antenna.Enabled = true;
      _oreDetector.Enabled = true;
    }
  }
  else Echo("Cannot undock: blocks missing!");
}
void DockingSequence()
{
  if (BlocksIntact())
  {
    Echo("Starting docking sequence...");
    SetCollisionAvoidance(true);
    SetPrecisionMode(true);
    SetThrusters(true);
    SetGyros(true);
    _aiRec.Enabled = true;
    _aiMove.Enabled = true;
  }
  else Echo("Cannot start docking sequence: blocks missing!");
}
void Dock()
{
  if (BlocksIntact())
  {
    Echo("Docking...");
    SetCollisionAvoidance(true);
    SetPrecisionMode(true);
    SetThrusters(true);
    SetGyros(true);
    _connector.Connect();
    if (_connector.IsConnected) {
      _antenna.Enabled = false;
      _oreDetector.Enabled = false;
      _connector.Connect();
      _aiMove.Enabled = false;
      _aiRec.Enabled = false;
      _gyros.ForEach(g => g.Enabled = false);
      _thrusters.ForEach(t => t.Enabled = false);
    }
  }
  else Echo("Cannot dock: blocks missing!");
}
// -- Block Helpers --------------------------------------------------------------
void SetGyros(bool on)
{
    for (int i = 0; i < _gyros.Count;     i++) _gyros[i].Enabled     = on;
}
void SetThrusters(bool on)
{
    for (int i = 0; i < _thrusters.Count; i++) _thrusters[i].Enabled = on;
}
void SetCollisionAvoidance(bool on)
{
    // IMyFlightMovementBlock exposes CollisionAvoidance as a direct bool property.
    if (_aiMove != null) _aiMove.CollisionAvoidance = on;
}
void SetPrecisionMode(bool on)
{
    // IMyFlightMovementBlock exposes PrecisionMode as a direct bool property.
    if (_aiMove != null) _aiMove.PrecisionMode = on;
}
// -- Target Display -------------------------------------------------------------
void AquireTarget(int distance)
{
	_sb.Clear();
	_sb.AppendLine("== Probe ==");
  if (ScopeCamera == null)
  {
    _sb.AppendLine("No camera found: " + ScopeCameraKeyword);
	}
	else {
		MyDetectedEntityInfo info = GetCameraTarget(ScopeCamera, distance);
		if (info.IsEmpty()) 
		{
			_sb.AppendLine("No target in sight");
			_sb.AppendLine(string.Format("Scanned distance: {0:N2} km", distance/1000.0));
		}
		else
		{
			_sb.AppendLine(string.Format("Target:   {0}", info.Name));
			_sb.AppendLine(string.Format("Type:     {0}", info.Type));
			_sb.AppendLine(string.Format("Velocity: {0:N2} m/s", info.Velocity.Length()));
			_sb.AppendLine(string.Format("Distance: {0:N2} m", Vector3D.Distance(ScopeCamera.GetPosition(), info.Position)));
			_sb.AppendLine(CreateGPS("Target", info.HitPosition));
		}
				
		_sb.AppendLine(string.Format("Scan range: {0} km", ScopeCamera.AvailableScanRange / 1000));
		_sb.AppendLine(string.Format("Scan cooldown: {0} s", ScopeCamera.TimeUntilScan(20000) / 1000));
		_sb.AppendLine(string.Format("Distance limit: {0}", ScopeCamera.RaycastDistanceLimit));
		_sb.AppendLine(string.Format("Time multiplier: {0}", ScopeCamera.RaycastTimeMultiplier));
	}
	for (int i = 0; i < ScopeDisplay.Count; i++)
	{
		WriteToDisplay(GetTextSurface(ScopeDisplay[i],ScopeDisplayPanel), true);
	}
}
// -- Status Display -------------------------------------------------------------
void WriteStatus()
{
    _sb.Clear();
    _sb.AppendLine("=== TSH Docking Controller ===");
    if (_connector != null) _sb.AppendLine(string.Format("Connector: {0}", _connector.Status));
    if (_battery   != null) _sb.AppendLine(string.Format("Battery:   {0}", _battery.ChargeMode));
    if (_aiMove    != null) _sb.AppendLine(string.Format("AI Move:   {0}", _aiMove.IsAutoPilotEnabled ? "Active" : "Idle"));
    if (_aiRec     != null) _sb.AppendLine(string.Format("AI Rec:    {0}", _aiRec.Enabled ? "On"     : "Off"));
    if (_antenna   != null) _sb.AppendLine(string.Format("Antenna:   {0}", _antenna.Enabled ? "On" : "Off"));
    if (_cockpit   != null) _sb.AppendLine(string.Format("Speed:     {0:F2} m/s", _cockpit.GetShipVelocities().LinearVelocity.Length()));
    string text = _sb.ToString();
    Echo(text);
    Me.GetSurface(0).WriteText(text);
}
// -- Block Cache ----------------------------------------------------------------
bool BlocksIntact()
{
    return _connector != null
        && _battery != null
        && _cockpit != null
        && _aiMove != null
        && _aiRec != null
        && _gyros.Count > 0
        && _antenna != null
        && _thrusters.Count > 0;
}
void RefreshBlockCache()
{
    _connector = GetTyped<IMyShipConnector>(PREFIX);
    _battery   = GetTyped<IMyBatteryBlock>(PREFIX);
    _cockpit   = GetTyped<IMyShipController>(PREFIX);
    _aiMove    = GetTyped<IMyFlightMovementBlock>(PREFIX); 
    _aiRec     = GetTyped<IMyPathRecorderBlock>(PREFIX); 
    _antenna   = GetTyped<IMyRadioAntenna>(PREFIX);
    _oreDetector = GetTyped<IMyOreDetector>(PREFIX);
    ScopeCamera  = GetTyped<IMyCameraBlock>(ScopeCameraKeyword);
    ScopeDisplay.Add(_cockpit as IMyTextSurfaceProvider);
    //_aiRecComp = _aiRec.Components.Get<IMyPathRecorderComponent>();

    _gyros.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyGyro>(_gyros,
        b => StringContains(b.CustomName, PREFIX));

    _thrusters.Clear();
    GridTerminalSystem.GetBlocksOfType<IMyThrust>(_thrusters,
        b => StringContains(b.CustomName, PREFIX));

    Echo(string.Format(
        "Cache - conn:{0} bat:{1} gyros:{2} thrust:{3} aiMove:{4} aiRec:{5} antenna:{6}",
        _connector != null ? "OK" : "!",
        _battery   != null ? "OK" : "!",
        _gyros.Count, _thrusters.Count,
        _aiMove    != null ? "OK" : "!",
        _aiRec     != null ? "OK" : "!",
        _antenna   != null ? "OK" : "!"));
}
// -- SEMF Utilities -------------------------------------------------------------
string CreateGPS(string name, Vector3D? pos)
{
	if (pos == null) return $"GPS:{name}:::::#FF00FF00:";
	return $"GPS:{name}:{pos.Value.X:F2}:{pos.Value.Y:F2}:{pos.Value.Z:F2}:#FF00FF00:";
}
bool StringContains(string source, string keyword)
{
    return source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
}
IMyTerminalBlock BlockNamed(string str)
{
    L.Clear();
    GridTerminalSystem.SearchBlocksOfName(str, L);
    return L.Count > 0 ? L[0] : null;
}
List<IMyTerminalBlock> BlocksNamed(string str)
{
    L.Clear();
    GridTerminalSystem.SearchBlocksOfName(str, L);
    return L;
}
IMyTextSurface GetTextSurface(IMyTextSurfaceProvider block, int panel = 0)
{
    return (block != null) ? block.GetSurface(panel) : null;
}
T GetTyped<T>(string nameFilter) where T : class, IMyTerminalBlock
{
    var tmp = new List<T>();
    GridTerminalSystem.GetBlocksOfType<T>(tmp, b => StringContains(b.CustomName, nameFilter));
    return tmp.Count > 0 ? tmp[0] : null;
}
void WriteToDisplay(IMyTextSurface surface = null, bool echoToo = false)
{
  if (surface != null)
  {
	surface.ContentType = ContentType.TEXT_AND_IMAGE;
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
void FormatAllDisplays(IMyTextSurfaceProvider DisplayBlock, bool setContentType = true)
{
	for (int i = 0; i < DisplayBlock.SurfaceCount; i++)
	{
		var surface = GetTextSurface(DisplayBlock, i);
		if (surface != null)
		{
			if (setContentType) surface.ContentType = ContentType.TEXT_AND_IMAGE;
			surface.Font = font;
			surface.FontColor = foregroundColor;
			surface.BackgroundColor = backgroundColor;

			surface.ScriptBackgroundColor = backgroundColor;
			surface.ScriptForegroundColor = foregroundColor;
		}
	}
}
MyDetectedEntityInfo GetCameraTarget(IMyCameraBlock camera, double distance = 5000)
{
	if (!camera.EnableRaycast)
		camera.EnableRaycast = true;
	if (camera.CanScan(distance)) return camera.Raycast((float)distance);
	return new MyDetectedEntityInfo();
}

