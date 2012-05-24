namespace System.Data.Entity.Core.Query.PlanCompiler
{
    using System.Data.Entity.Core.Query.InternalTrees;

    /// <summary>
    /// Utility class to gather helper methods used by more than one class in the Aggregate Pushdown feature.
    /// </summary>
    internal static class AggregatePushdownUtil
    {
        /// <summary>
        /// Determines whether the given node is a VarRef over the given var
        /// </summary>
        /// <param name="node"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        internal static bool IsVarRefOverGivenVar(Node node, Var var)
        {
            if (node.Op.OpType
                != OpType.VarRef)
            {
                return false;
            }
            return ((VarRefOp)node.Op).Var == var;
        }
    }
}
