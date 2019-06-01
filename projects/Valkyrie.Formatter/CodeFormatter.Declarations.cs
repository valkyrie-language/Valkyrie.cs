using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.Shader;
using Oak.Valkyrie.AST.Term;

namespace Valkyrie.Formatter;

public sealed partial class CodeFormatter
{
    #region 声明格式化

    private void FormatNode(AstNode node)
    {
        switch (node)
        {
            case ImportDecl import:
                FormatImportDecl(import);
                break;
            case VariableDecl varDecl:
                FormatVariableDecl(varDecl);
                break;
            case ComponentDecl compDecl:
                FormatComponentDecl(compDecl);
                break;
            case SystemDecl sysDecl:
                FormatSystemDecl(sysDecl);
                break;
            case WidgetDecl widgetDecl:
                FormatWidgetDecl(widgetDecl);
                break;
            case PluginDecl pluginDecl:
                FormatPluginDecl(pluginDecl);
                break;
            case FunctionDecl funcDecl:
                FormatFunctionDecl(funcDecl);
                break;
            case ShaderDecl shaderDecl:
                FormatShaderDecl(shaderDecl);
                break;
            case StructureDecl structDecl:
                FormatStructDecl(structDecl);
                break;
            case UniformDecl uniformDecl:
                FormatUniformDecl(uniformDecl);
                break;
            case VaryingDecl varyingDecl:
                FormatVaryingDecl(varyingDecl);
                break;
            case ConstantBufferDecl cbufferDecl:
                FormatConstantBufferDecl(cbufferDecl);
                break;
            case TextureDecl textureDecl:
                FormatTextureDecl(textureDecl);
                break;
            case SamplerDecl samplerDecl:
                FormatSamplerDecl(samplerDecl);
                break;
            case UniformBindingDecl uniformBindingDecl:
                FormatUniformBindingDecl(uniformBindingDecl);
                break;
            case ShaderAttributeDecl shaderAttrDecl:
                FormatShaderAttributeDecl(shaderAttrDecl);
                break;
            case BlockStmt block:
                FormatBlockStmt(block);
                break;
            case IfStatement ifStmt:
                FormatIfStmt(ifStmt);
                break;
            case LoopStmt loopStmt:
                FormatLoopStmt(loopStmt);
                break;
            case WhileStatement whileStmt:
                FormatWhileStmt(whileStmt);
                break;
            case MatchStmt matchStmt:
                FormatMatchStmt(matchStmt);
                break;
            case CatchStmt catchStmt:
                FormatCatchStmt(catchStmt);
                break;
            case ReturnStatement retStmt:
                FormatReturnStmt(retStmt);
                break;
            case ResumeStatement resumeStmt:
                FormatResumeStmt(resumeStmt);
                break;
            case TermExpressionStatement exprStmt:
                FormatExpressionStmt(exprStmt);
                break;
            case DiscardStmt:
                FormatDiscardStmt();
                break;
            default:
                break;
        }
    }

    private void FormatImportDecl(ImportDecl import)
    {
        WriteIndent();
        Write("import ");
        Write(import.ModulePath);

        if (import.Alias is not null)
        {
            Write(" as ");
            Write(import.Alias);
        }

        WriteSemicolon();
        NewLine();
    }

    private void FormatVariableDecl(VariableDecl varDecl)
    {
        WriteIndent();
        Write("let ");

        if (varDecl.IsMutable)
        {
            Write("mut ");
        }

        Write(varDecl.Name);

        if (varDecl.VarType is not null)
        {
            Write(": ");
            WriteTypeAnnotation(varDecl.VarType);
        }

        if (varDecl.Initializer is not null)
        {
            Write(" = ");
            FormatExpression(varDecl.Initializer);
        }

        WriteSemicolon();
        NewLine();
    }

    private void FormatComponentDecl(ComponentDecl compDecl)
    {
        FormatAttributes(compDecl.Attributes);

        WriteIndent();
        Write("component ");
        Write(compDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;
        foreach (var field in compDecl.Fields)
        {
            FormatFieldDecl(field);
        }
        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatFieldDecl(FieldDecl field)
    {
        WriteIndent();

        FormatAttributesInline(field.Attributes);

        Write(field.Name);
        Write(": ");
        WriteTypeAnnotation(field.FieldType);

        if (field.DefaultValue is not null)
        {
            Write(" = ");
            FormatExpression(field.DefaultValue);
        }

        WriteSemicolon();
        NewLine();
    }

    private void FormatSystemDecl(SystemDecl sysDecl)
    {
        FormatAttributes(sysDecl.Attributes);

        WriteIndent();
        Write("system ");
        Write(sysDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var query in sysDecl.Queries)
        {
            FormatQueryDecl(query);
        }

        foreach (var method in sysDecl.Methods)
        {
            FormatFunctionDecl(method);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatWidgetDecl(WidgetDecl widgetDecl)
    {
        WriteIndent();
        Write("widget ");
        Write(widgetDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var prop in widgetDecl.Properties)
        {
            FormatFieldDecl(prop);
        }

        if (widgetDecl.RenderMethod is not null)
        {
            NewLine();
            FormatFunctionDecl(widgetDecl.RenderMethod);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatPluginDecl(PluginDecl pluginDecl)
    {
        WriteIndent();
        Write("plugin ");
        Write(pluginDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        FormatPluginField("requires_arch", pluginDecl.RequiresArch is not null
            ? [pluginDecl.RequiresArch]
            : []);
        FormatPluginField("provides_macros", pluginDecl.ProvidesMacros);
        FormatPluginField("provides_capabilities", pluginDecl.ProvidesCapabilities);

        foreach (var func in pluginDecl.Functions)
        {
            FormatFunctionDecl(func);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatPluginField(string name, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        WriteIndent();
        Write(name);
        Write(" = [");

        for (int i = 0; i < values.Count; i++)
        {
            Write("\"");
            Write(values[i]);
            Write("\"");

            if (i < values.Count - 1)
            {
                Write(", ");
            }
        }

        Write("]");
        WriteSemicolon();
        NewLine();
    }

    private void FormatFunctionDecl(FunctionDecl funcDecl)
    {
        FormatAttributes(funcDecl.Attributes);

        WriteIndent();
        Write("micro ");
        Write(funcDecl.Name);
        Write("(");

        for (int i = 0; i < funcDecl.Parameters.Count; i++)
        {
            FormatParameterDecl(funcDecl.Parameters[i]);

            if (i < funcDecl.Parameters.Count - 1)
            {
                Write(",");
                if (_config.SpaceAfterComma)
                {
                    Write(" ");
                }
            }
        }

        Write(")");

        if (funcDecl.ReturnType is not null)
        {
            Write(": ");
            WriteTypeAnnotation(funcDecl.ReturnType);
        }

        Write(" ");

        if (funcDecl.Body is not null)
        {
            FormatBlockStmt(funcDecl.Body);
        }
        else
        {
            WriteSemicolon();
            NewLine();
        }
    }

    private void FormatParameterDecl(ParameterDecl param)
    {
        FormatAttributesInline(param.Attributes);
        Write(param.Name);
        Write(": ");
        WriteTypeAnnotation(param.ParamType);
    }

    private void FormatQueryDecl(QueryExpr query)
    {
        WriteIndent();
        Write("query ");

        string kind = query.Kind switch
        {
            QueryKind.All => "all",
            QueryKind.Any => "any",
            QueryKind.None => "none",
            _ => "all"
        };

        Write(kind);
        Write(" = Query.");
        Write(kind);
        Write("(");

        for (int i = 0; i < query.ComponentTypes.Count; i++)
        {
            WriteTypeAnnotation(query.ComponentTypes[i]);

            if (i < query.ComponentTypes.Count - 1)
            {
                Write(",");
                if (_config.SpaceAfterComma)
                {
                    Write(" ");
                }
            }
        }

        Write(")");
        WriteSemicolon();
        NewLine();
    }

    #endregion

    #region 着色器声明格式化

    private void FormatShaderDecl(ShaderDecl shaderDecl)
    {
        FormatAttributes(shaderDecl.Attributes);

        WriteIndent();
        Write("shader ");
        Write(shaderDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var stage in shaderDecl.Stages)
        {
            FormatShaderStageDecl(stage);

            if (stage != shaderDecl.Stages[^1])
            {
                NewLine();
            }
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatShaderStageDecl(ShaderStageDecl stage)
    {
        WriteIndent();

        string keyword = stage switch
        {
            VertexShaderDecl => "vertex",
            FragmentShaderDecl => "fragment",
            ComputeShaderDecl => "compute",
            _ => "stage"
        };

        Write(keyword);
        Write(" ");
        Write(stage.Name);
        Write(" ");

        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var bodyNode in stage.Body)
        {
            FormatNode(bodyNode);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatStructDecl(StructureDecl structDecl)
    {
        WriteIndent();
        Write("structure ");
        Write(structDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var field in structDecl.Fields)
        {
            FormatFieldDecl(field);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatUniformDecl(UniformDecl uniformDecl)
    {
        WriteIndent();
        Write("uniform ");
        WriteTypeAnnotation(uniformDecl.UniformType);
        Write(" ");
        Write(uniformDecl.Name);
        WriteSemicolon();
        NewLine();
    }

    private void FormatVaryingDecl(VaryingDecl varyingDecl)
    {
        WriteIndent();
        Write("varying ");
        WriteTypeAnnotation(varyingDecl.VaryingType);
        Write(" ");
        Write(varyingDecl.Name);
        WriteSemicolon();
        NewLine();
    }

    private void FormatConstantBufferDecl(ConstantBufferDecl cbufferDecl)
    {
        WriteIndent();
        Write("cbuffer ");
        Write(cbufferDecl.Name);
        Write(" ");
        OpenBrace();
        NewLine();

        _indentLevel++;

        foreach (var field in cbufferDecl.Fields)
        {
            FormatFieldDecl(field);
        }

        _indentLevel--;

        WriteIndent();
        CloseBrace();
        NewLine();
    }

    private void FormatTextureDecl(TextureDecl textureDecl)
    {
        WriteIndent();
        Write("texture ");
        WriteTypeAnnotation(textureDecl.TextureType);
        Write(" ");
        Write(textureDecl.Name);
        WriteSemicolon();
        NewLine();
    }

    private void FormatSamplerDecl(SamplerDecl samplerDecl)
    {
        WriteIndent();
        Write("sampler ");
        Write(samplerDecl.Name);
        WriteSemicolon();
        NewLine();
    }

    private void FormatUniformBindingDecl(UniformBindingDecl uniformBindingDecl)
    {
        WriteIndent();
        WriteTypeAnnotation(uniformBindingDecl.BindingType);
        Write(" ");
        Write(uniformBindingDecl.Name);

        if (uniformBindingDecl.Group.HasValue || uniformBindingDecl.Binding.HasValue)
        {
            Write(" : register(");

            if (uniformBindingDecl.Group.HasValue)
            {
                Write($"u{uniformBindingDecl.Group.Value}");
            }

            if (uniformBindingDecl.Binding.HasValue)
            {
                Write($", b{uniformBindingDecl.Binding.Value}");
            }

            Write(")");
        }

        WriteSemicolon();
        NewLine();
    }

    private void FormatShaderAttributeDecl(ShaderAttributeDecl shaderAttrDecl)
    {
        WriteIndent();
        Write("attribute ");
        WriteTypeAnnotation(shaderAttrDecl.AttrType);
        Write(" ");
        Write(shaderAttrDecl.Name);

        if (shaderAttrDecl.Location.HasValue)
        {
            Write($" : location({shaderAttrDecl.Location.Value})");
        }

        WriteSemicolon();
        NewLine();
    }

    private void FormatDiscardStmt()
    {
        WriteIndent();
        Write("discard");
        WriteSemicolon();
        NewLine();
    }

    private void FormatSwizzleExpr(SwizzleExpr swizzle)
    {
        FormatExpression(swizzle.Target);
        Write(".");
        Write(swizzle.Components);
    }

    #endregion
}
