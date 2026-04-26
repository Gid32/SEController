// ScoutDrone - Navigation scanner script
// Trigger with argument "navscan" to scan and display target info.

string NavigationCameraKeyword;
string NavigationDisplayKeyword;
int    NavigationDisplayPanel;

Color  foregroundColor;
Color  backgroundColor;
const string font = "Monospace";

IMyTextSurface NavigationDisplay;
IMyCameraBlock NavigationCamera;

List<IMyTerminalBlock> L = new List<IMyTerminalBlock>();
System.Text.StringBuilder _sb = new System.Text.StringBuilder();

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.None;

    foregroundColor = new Color(100, 255, 100, 255);
    backgroundColor = new Color(0, 0, 0, 255);

    NavigationCameraKeyword = "[SD Scope Camera]";
    NavigationDisplayKeyword = "[Navigation]";
    NavigationDisplayPanel = 0;

    RefreshBlockCache();
}

public void Main(string argument, UpdateType updateSource)
{
  if (argument.IndexOf("navscan", StringComparison.OrdinalIgnoreCase) >= 0)
  {
		int distance = 20000;
    if (argument.Trim().Split(' ').Length >= 2) int.TryParse(argument.Trim().Split(' ')[1], out distance);

    RefreshBlockCache();
    DisplayNavigation(distance);
  }
}

void RefreshBlockCache()
{
    NavigationCamera  = BlockNamed(NavigationCameraKeyword) as IMyCameraBlock;
    NavigationDisplay = GetTextSurface(BlockNamed(NavigationDisplayKeyword) as IMyTextSurfaceProvider, NavigationDisplayPanel);
}

void DisplayNavigation(int distance)
{
  _sb.Clear();
  _sb.AppendLine("== Navigation ==");

  if (NavigationCamera == null)
  {
    _sb.AppendLine("No camera found: " + NavigationCameraKeyword);
    WriteToDisplay(NavigationDisplay);
    return;
  }

  MyDetectedEntityInfo info = GetCameraTarget(NavigationCamera, distance);
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
    _sb.AppendLine(string.Format("Distance: {0:N2} m", Vector3D.Distance(NavigationCamera.GetPosition(), info.Position)));
    _sb.AppendLine(CreateGPS("Target", info.HitPosition));
  }
  _sb.AppendLine(string.Format("Scan range:      {0} km", NavigationCamera.AvailableScanRange / 1000));
  _sb.AppendLine(string.Format("Scan cooldown:   {0} s", NavigationCamera.TimeUntilScan(20000) / 1000));
  _sb.AppendLine(string.Format("Distance limit:  {0}", NavigationCamera.RaycastDistanceLimit));
  _sb.AppendLine(string.Format("Time multiplier: {0}", NavigationCamera.RaycastTimeMultiplier));

  WriteToDisplay(NavigationDisplay);
}

void WriteToDisplay(IMyTextSurface surface = null)
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
  Echo(_sb.ToString());
}

MyDetectedEntityInfo GetCameraTarget(IMyCameraBlock camera, double distance = 20000)
{
  if (!camera.EnableRaycast)
    camera.EnableRaycast = true;
  if (camera.CanScan(distance)) return camera.Raycast((float)distance);
  return new MyDetectedEntityInfo();
}

string CreateGPS(string name, Vector3D? pos)
{
  if (pos == null) return $"GPS:{name}:::::#FF00FF00:";
  return $"GPS:{name}:{pos.Value.X:F2}:{pos.Value.Y:F2}:{pos.Value.Z:F2}:#FF00FF00:";
}

IMyTextSurface GetTextSurface(IMyTextSurfaceProvider block, int panel = 0)
{
  return (block != null) ? block.GetSurface(panel) : null;
}

List<IMyTerminalBlock> BlocksNamed(string str)
{
  L.Clear();
  GridTerminalSystem.SearchBlocksOfName(str, L);
  return L;
}

IMyTerminalBlock BlockNamed(string str)
{
  L = BlocksNamed(str);
  return L.Count > 0 ? L[0] : null;
}
