double OxygenLimit;
bool   IsProducer;
////////////////////////////////////// LISTS ///////////////////////////////////
List<IMyGasTank> oxygen = new List<IMyGasTank>();
List<IMyGasTank> hydrogen = new List<IMyGasTank>();
List<IMyGasGenerator> OOHHGenerators = new List<IMyGasGenerator>();
////////////////////////////////////// LISTS ///////////////////////////////////

public Program(){
	Runtime.UpdateFrequency = UpdateFrequency.Update100;
	
	OxygenLimit = 0.7;
	IsProducer = true;
}

public void Save(){
    // Called when the program needs to save its state. Use
    // this method to save your state to the Storage field
    // or some other means. 
    // 
    // This method is optional and can be removed if not
    // needed.
}

public void Main(string argument, UpdateType updateSource){
	Test();
}
// ------------------------------------------------------------------------------- OxyLimiter

void Test(){
	GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygen, x=>x.DetailedInfo.Split(' ')[1] == "Oxygen");
	GridTerminalSystem.GetBlocksOfType<IMyGasTank>(hydrogen, x=>x.DetailedInfo.Split(' ')[1] == "Hydrogen");
	GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(OOHHGenerators);
	
	if(IsProducer)
	{
		oxygen   =  SameConstruct(ref oxygen);
		hydrogen =	SameConstruct(ref hydrogen);
	}
	
	Echo("Test");
	
	

}

List<T> SameConstruct<T>(ref List<T> blocks) where T : IMyTerminalBlock{
	return blocks.Where(block=>IsSameConstruct(block)).ToList();
}
List<T> AnotherConstruct<T>(ref List<T> blocks) where T : IMyTerminalBlock{
	return blocks.Where(block=>!IsSameConstruct(block)).ToList();
}

bool IsSameConstruct (IMyTerminalBlock block) {
	return block.IsSameConstructAs(Me);
}
// ------------------------------------------------------------------------------- util


void MoveAllItems(IMyInventory source, IMyInventory dest){
	List<MyInventoryItem> items = new List<MyInventoryItem>();
	source.GetItems(items);
	
	for (int i = items.Count -1; i >= 0; i--)  
	{  
		source.TransferItemTo(dest, i, null, true, null);  
	}
}

double[] GetXYZ(IMyTerminalBlock block){   
    return new double[]{block.GetPosition().GetDim(0), block.GetPosition().GetDim(1), block.GetPosition().GetDim(2)};   
}   
   
void SetName(IMyTerminalBlock block, string name){   
    if (block != null) {    
        block.CustomName = name;    
    }   
}   
   
List<IMyTerminalBlock> BlocksNamed(String str){   
    List<IMyTerminalBlock> L = new List<IMyTerminalBlock>();   
    GridTerminalSystem.SearchBlocksOfName(str,L);   
    return L;   
}   

IMyTerminalBlock BlockNamed(String str){   
    List<IMyTerminalBlock> L = BlocksNamed(str);   
    return L.Count>0?L[0]:null;   
}   

void assert(bool cond, String errormsg){   
    if(!cond) throw new Exception(errormsg);   
}