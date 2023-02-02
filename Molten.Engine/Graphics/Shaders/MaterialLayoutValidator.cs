﻿namespace Molten.Graphics
{
    public class MaterialLayoutValidator
    {
        public bool Validate(ShaderCompilerContext context,
            MaterialPassCompileResult pResult)
        {
            // Stage order reference: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476882(v=vs.85).aspx
            bool valid = true;
            MaterialPass pass = pResult.Pass;
            ShaderComposition[] stages = pass.Compositions;
            ShaderComposition previous = null;

            for (int i = 0; i < stages.Length; i++)
            {
                // Nothing to compare yet, continue.
                if (previous == null)
                {
                    previous = stages[i];
                    continue;
                }

                // No shader to compare. Go to next shader stage.
                if (string.IsNullOrWhiteSpace(stages[i].EntryPoint))
                    continue;

                ShaderIOStructure output = previous.OutputStructure;
                ShaderIOStructure input = stages[i].InputStructure;

                // If the input expects anything, check compatibility. Skip compat check if input does not expect anything (length 0).
                if (input.Metadata.Length > 0 && !output.IsCompatible(input))
                {
                    ShaderType currentCompositionType =  stages[i].Type;
                    ShaderType previousCompositionType = previous.Type;

                    context.AddError("Incompatible material I/O structure.");
                    context.AddError("====================================");
                    context.AddError($"\tFilename: {pass.Material.Filename ?? "N/A"}");
                    context.AddError($"\tOutput -- {previousCompositionType}:");

                    if (output.Metadata.Length > 0)
                    {
                        for (int o = 0; o < output.Metadata.Length; o++)
                            context.AddError($"\t\t[{o}] {output.Metadata[o].SystemValueType} -- index: {output.Metadata[o].SemanticIndex}");
                    }
                    else
                    {
                        context.AddError("No output elements expected.");
                    }

                    context.AddError($"\tInput: {currentCompositionType}:");
                    for (int o = 0; o < input.Metadata.Length; o++)
                        context.AddError($"\t\t[{o}] {input.Metadata[o].SystemValueType} -- index: {input.Metadata[o].SemanticIndex}");

                    valid = false;
                }

                previous = stages[i];
            }

            return valid &&
                CheckTessellationShaders(context, pResult) &&
                CheckGeometryTessellationAdjacency(pResult);
        }

        private bool CheckTessellationShaders(
            ShaderCompilerContext context,
            MaterialPassCompileResult pResult)
        {
            bool valid = true;
            ShaderClassResult hs = pResult[ShaderType.Hull];
            ShaderClassResult ds = pResult[ShaderType.Domain];

            if (hs != null && ds == null)
            {
                context.AddError($"Material pass '{pResult.Pass.Name}' Has a hull shader but no domain shader. Both or neither must be present.");
                valid = false;
            }
            else if (hs == null && ds != null)
            {
                context.AddError($"Material pass '{pResult.Pass.Name}' Has a domain shader but no hull shader. Both or neither must be present.");
                valid = false;
            }

            return valid;
        }

        private bool CheckGeometryTessellationAdjacency(MaterialPassCompileResult pResult)
        {
            bool valid = true;
            ShaderClassResult geometryRef = pResult[ShaderType.Geometry];
            ShaderClassResult hullRef = pResult[ShaderType.Hull];
            ShaderClassResult domainRef = pResult[ShaderType.Domain];

            if (geometryRef == null || hullRef == null || domainRef == null)
                return valid;

            /* Invalidate if tessellation is active with a geometry shader that expects adjacency data.
                * see: https://msdn.microsoft.com/en-us/library/windows/desktop/ff476340%28v=vs.85%29.aspx
                * quote: "A geometry shader that expects primitives with adjacency (for example, 6 vertices per triangle) is 
                * not valid when tessellation is active (this results in undefined behavior, which the debug layer will complain about)."*/
            valid = pResult.Pass.GeometryPrimitive == PrimitiveTopology.LineAdj ||
                pResult.Pass.GeometryPrimitive == PrimitiveTopology.TriangleAdj;

            return valid;
        }
    }
}