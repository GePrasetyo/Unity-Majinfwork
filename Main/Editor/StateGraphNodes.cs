#if HAS_STATEGRAPH
using System;
using Majinfwork.StateGraph;

namespace Majinfwork {
    [Serializable] public class LoadLevelNode : StateNode<LoadLevel> { }
    [Serializable] public class LoadLevelAddressableNode : StateNode<LoadLevelAddressable> { }
}
#endif
