using Oak.Valkyrie.AST;
using Oak.Valkyrie.AST.ECS;
using Oak.Valkyrie.AST.HAL;
using Oak.Valkyrie.AST.PAL;
using Oak.Valkyrie.AST.Shader;

namespace Valkyrie.TypeChecker;

/// <summary>
/// 声明检查：变量、函数、组件、系统、Widget、Plugin 以及 Shader 相关声明
/// </summary>
public sealed partial class TypeChecker
{
    #region 声明检查
    private void CheckImportDecl(ImportDecl import)
    {
        var symbol = new Symbol(import.Alias ?? import.ModulePath, ValkyrieType.Auto, SymbolKind.Import);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2001", $"导入 '{import.ModulePath}' 与已有符号冲突", import.Span, "使用 import as 别名避免冲突，或移除重复导入");
        }
    }

    private void CheckUsingDecl(UsingDecl usingDecl)
    {
        var symbol = new Symbol(usingDecl.Alias ?? usingDecl.ModulePath, ValkyrieType.Auto, SymbolKind.Import);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2001", $"using '{usingDecl.ModulePath}' 与已有符号冲突", usingDecl.Span, "使用 using as 别名避免冲突，或移除重复 using");
        }
    }

    private void CheckVariableDecl(VariableDecl varDecl)
    {
        var initializerType = varDecl.Initializer is not null
            ? InferType(varDecl.Initializer)
            : ValkyrieType.Error;

        if (varDecl.VarType is not null)
        {
            var declaredType = ResolveTypeAnnotation(varDecl.VarType);

            if (!declaredType.IsError && !initializerType.IsError
                                      && varDecl.Initializer is not null
                                      && !declaredType.IsAssignableFrom(initializerType))
            {
                AddError("VALK2002",
                    $"变量 '{varDecl.Name}' 类型不匹配：声明为 '{declaredType}'，但初始值为 '{initializerType}'",
                    varDecl.Span, "将初始值类型改为与注解类型一致，或移除类型注解让编译器推断");
            }

            if (varDecl.Initializer is null)
            {
                var symbol = new Symbol(varDecl.Name, declaredType, SymbolKind.Variable, varDecl.IsMutable);
                if (!_currentScope.Define(symbol))
                {
                    AddError("VALK2003", $"变量 '{varDecl.Name}' 已在当前作用域中定义", varDecl.Span, "重命名变量以避免重复定义");
                }

                return;
            }
        }

        var variableType = varDecl.VarType is not null ? ResolveTypeAnnotation(varDecl.VarType) : initializerType;
        var varSymbol = new Symbol(varDecl.Name, variableType, SymbolKind.Variable, varDecl.IsMutable);

        if (!_currentScope.Define(varSymbol))
        {
            AddError("VALK2003", $"变量 '{varDecl.Name}' 已在当前作用域中定义", varDecl.Span, "重命名变量以避免重复定义");
        }
    }

    private void CheckComponentDecl(ComponentDecl compDecl)
    {
        var fields = new Dictionary<string, ValkyrieType>(StringComparer.Ordinal);

        foreach (var field in compDecl.Fields)
        {
            var fieldType = ResolveTypeAnnotation(field.FieldType);

            if (field.DefaultValue is not null)
            {
                var defaultType = InferType(field.DefaultValue);
                if (!fieldType.IsError && !defaultType.IsError && !fieldType.IsAssignableFrom(defaultType))
                {
                    AddError("VALK2004",
                        $"组件 '{compDecl.Name}' 的字段 '{field.Name}' 默认值类型不匹配：期望 '{fieldType}'，实际 '{defaultType}'",
                        field.Span, "将默认值类型改为与字段类型一致");
                }
            }

            if (fields.ContainsKey(field.Name))
            {
                AddError("VALK2005", $"组件 '{compDecl.Name}' 中字段 '{field.Name}' 重复定义", field.Span, "重命名字段以避免重复定义");
            }
            else
            {
                fields[field.Name] = fieldType;
            }
        }

        var compType = new ValkyrieType(TypeKind.Component, compDecl.Name);
        _typeRegistry[compDecl.Name] = compType;

        var symbol = new Symbol(compDecl.Name, compType, SymbolKind.Component);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2006", $"组件 '{compDecl.Name}' 已在当前作用域中定义", compDecl.Span, "重命名组件以避免重复定义");
        }
    }

    private void CheckSystemDecl(SystemDecl sysDecl)
    {
        foreach (var query in sysDecl.Queries)
        {
            CheckQueryExpr(query);
        }

        var childScope = new Scope(_currentScope);

        foreach (var method in sysDecl.Methods)
        {
            CheckFunctionDeclInScope(method, childScope);
        }

        var sysType = new ValkyrieType(TypeKind.System, sysDecl.Name);
        _typeRegistry[sysDecl.Name] = sysType;

        var symbol = new Symbol(sysDecl.Name, sysType, SymbolKind.System);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2007", $"系统 '{sysDecl.Name}' 已在当前作用域中定义", sysDecl.Span, "重命名系统以避免重复定义");
        }
    }

    private void CheckWidgetDecl(WidgetDecl widgetDecl)
    {
        foreach (var prop in widgetDecl.Properties)
        {
            ResolveTypeAnnotation(prop.FieldType);
        }

        if (widgetDecl.RenderMethod is not null)
        {
            var childScope = new Scope(_currentScope);
            CheckFunctionDeclInScope(widgetDecl.RenderMethod, childScope);
        }

        var widgetType = new ValkyrieType(TypeKind.Widget, widgetDecl.Name);
        _typeRegistry[widgetDecl.Name] = widgetType;

        var symbol = new Symbol(widgetDecl.Name, widgetType, SymbolKind.Widget);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2008", $"Widget '{widgetDecl.Name}' 已在当前作用域中定义", widgetDecl.Span, "重命名 Widget 以避免重复定义");
        }
    }

    private void CheckPluginDecl(PluginDecl pluginDecl)
    {
        var childScope = new Scope(_currentScope);

        foreach (var func in pluginDecl.Functions)
        {
            CheckFunctionDeclInScope(func, childScope);
        }

        var pluginType = new ValkyrieType(TypeKind.Plugin, pluginDecl.Name);
        _typeRegistry[pluginDecl.Name] = pluginType;

        var symbol = new Symbol(pluginDecl.Name, pluginType, SymbolKind.Plugin);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2009", $"Plugin '{pluginDecl.Name}' 已在当前作用域中定义", pluginDecl.Span, "重命名 Plugin 以避免重复定义");
        }
    }

    private void CheckFunctionDecl(FunctionDecl funcDecl)
    {
        CheckFunctionDeclInScope(funcDecl, _currentScope);
    }

    private void CheckFunctionDeclInScope(FunctionDecl funcDecl, Scope parentScope)
    {
        var paramTypes = new List<ParameterType>();

        var funcScope = new Scope(parentScope);

        var prevScope = _currentScope;
        _currentScope = funcScope;

        foreach (var tp in funcDecl.TypeParameters)
        {
            var constraintNames = funcDecl.GenericConstraints
                .Where(c => c.ParameterName == tp.Name)
                .SelectMany(c => c.ConstraintTypes)
                .Select(c => c.Name)
                .ToList();

            var typeVar = ValkyrieType.TypeVariable(tp.Name, constraintNames);
            var typeSymbol = new Symbol(tp.Name, typeVar, SymbolKind.Variable);
            funcScope.Define(typeSymbol);
        }

        if (funcDecl.GenericConstraints.Count > 0)
        {
            CheckTypeParameterConstraintsEnhanced(funcDecl.TypeParameters, funcDecl.GenericConstraints);
        }

        foreach (var param in funcDecl.Parameters)
        {
            var paramType = ResolveTypeAnnotation(param.ParamType);
            paramTypes.Add(new ParameterType(param.Name, paramType));

            var paramSymbol = new Symbol(param.Name, paramType, SymbolKind.Parameter);
            if (!funcScope.Define(paramSymbol))
            {
                AddError("VALK2010", $"参数 '{param.Name}' 在函数 '{funcDecl.Name}' 中重复定义", param.Span, "重命名参数以避免重复定义");
            }
        }

        var returnType = funcDecl.ReturnType is not null
            ? ResolveTypeAnnotation(funcDecl.ReturnType)
            : ValkyrieType.Unit;

        var genericTypeParamNames = funcDecl.TypeParameters.Select(t => t.Name).ToList();
        var genericConstraintNames = funcDecl.GenericConstraints
            .Select(c => $"{c.ParameterName}:{string.Join(",", c.ConstraintTypes.Select(ct => ct.Name))}")
            .ToList();

        var effects = ConvertEffectSpecifier(funcDecl.EffectSpecifier);

        var funcType = new ValkyrieType(TypeKind.Function, funcDecl.Name,
            parameters: paramTypes, returnType: returnType,
            genericTypeParams: genericTypeParamNames,
            genericConstraintNames: genericConstraintNames,
            effects: effects);

        var funcSymbol = new Symbol(funcDecl.Name, funcType, SymbolKind.Function);
        if (!parentScope.Define(funcSymbol))
        {
            AddError("VALK2011", $"函数 '{funcDecl.Name}' 已在当前作用域中定义", funcDecl.Span, "重命名函数以避免重复定义");
        }

        if (funcDecl.Body is not null)
        {
            var savedScope = _currentScope;
            var prevFunction = _currentFunction;
            _currentScope = funcScope;
            _currentFunction = funcDecl;
            CheckBlockStmt(funcDecl.Body);
            _currentScope = savedScope;
            _currentFunction = prevFunction;
        }
    }

    #endregion

    #region 枚举与联合类型检查

    /// <summary>
    /// 检查 Trait 声明：验证方法签名、注册 trait 类型
    /// </summary>
    private void CheckTraitDecl(TraitDecl traitDecl)
    {
        var traitScope = new Scope(_currentScope);

        foreach (var tp in traitDecl.TypeParameters)
        {
            var constraintNames = traitDecl.GenericConstraints
                .Where(c => c.ParameterName == tp.Name)
                .SelectMany(c => c.ConstraintTypes)
                .Select(c => c.Name)
                .ToList();

            var typeVar = ValkyrieType.TypeVariable(tp.Name, constraintNames);
            var typeSymbol = new Symbol(tp.Name, typeVar, SymbolKind.Variable);
            traitScope.Define(typeSymbol);
        }

        if (traitDecl.GenericConstraints.Count > 0)
        {
            CheckTypeParameterConstraintsEnhanced(traitDecl.TypeParameters, traitDecl.GenericConstraints);
        }

        foreach (var method in traitDecl.Methods)
        {
            var paramTypes = new List<ParameterType>();
            foreach (var param in method.Parameters)
            {
                var paramType = ResolveTypeAnnotation(param.ParamType);
                paramTypes.Add(new ParameterType(param.Name, paramType));
            }

            var returnType = method.ReturnType is not null
                ? ResolveTypeAnnotation(method.ReturnType)
                : ValkyrieType.Unit;

            var methodType = new ValkyrieType(TypeKind.Function, method.Name,
                parameters: paramTypes, returnType: returnType);

            var methodSymbol = new Symbol(method.Name, methodType, SymbolKind.Function);
            if (!traitScope.Define(methodSymbol))
            {
                AddWarning("VALK2085",
                    $"Trait '{traitDecl.Name}' 中方法 '{method.Name}' 重复定义",
                    method.Span);
            }
        }

        var traitType = ValkyrieType.TraitType(traitDecl.Name);
        RegisterTraitDecl(traitDecl.Name, traitType);
    }

    /// <summary>
    /// 检查枚举声明：验证成员值类型一致性，注册枚举类型和成员符号    /// </summary>
    private void CheckEnumDecl(EnumDecl enumDecl)
    {
        var variantNames = new HashSet<string>(StringComparer.Ordinal);
        var variantTypes = new List<ValkyrieType>();

        foreach (var member in enumDecl.Members)
        {
            if (!variantNames.Add(member.Name))
            {
                AddError("VALK2060",
                    $"枚举 '{enumDecl.Name}' 中成员 '{member.Name}' 重复定义",
                    member.Span, "重命名枚举成员以避免重复定义");
                continue;
            }

            if (member.Value is not null)
            {
                var valueType = InferType(member.Value);
                if (!valueType.IsError && !valueType.IsInteger && !valueType.IsString)
                {
                    AddError("VALK2061",
                        $"枚举 '{enumDecl.Name}' 的成员 '{member.Name}' 值类型不合法：期望整数或字符串，实际为 '{valueType}'",
                        member.Span, "枚举成员值必须是整数或字符串字面量");
                }
            }

            var memberType = new ValkyrieType(TypeKind.Enum, $"{enumDecl.Name}.{member.Name}");
            variantTypes.Add(memberType);

            var memberSymbol = new Symbol(member.Name, memberType, SymbolKind.Variable);
            if (!_currentScope.Define(memberSymbol))
            {
                AddWarning("VALK2062",
                    $"枚举成员 '{member.Name}' 与外层作用域符号冲突",
                    member.Span);
            }
        }

        var enumType = new ValkyrieType(TypeKind.Enum, enumDecl.Name, variantTypes);
        _typeRegistry[enumDecl.Name] = enumType;

        var symbol = new Symbol(enumDecl.Name, enumType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2063",
                $"枚举 '{enumDecl.Name}' 已在当前作用域中定义",
                enumDecl.Span, "重命名枚举以避免重复定义");
        }
    }

    /// <summary>
    /// 检查位标志声明：验证成员值为整数，注册标志类型和成员符号
    /// </summary>
    private void CheckFlagsDecl(FlagsDecl flagsDecl)
    {
        var variantNames = new HashSet<string>(StringComparer.Ordinal);
        var variantTypes = new List<ValkyrieType>();

        foreach (var member in flagsDecl.Members)
        {
            if (!variantNames.Add(member.Name))
            {
                AddError("VALK2064",
                    $"位标志 '{flagsDecl.Name}' 中成员 '{member.Name}' 重复定义",
                    member.Span, "重命名标志成员以避免重复定义");
                continue;
            }

            if (member.Value is not null)
            {
                var valueType = InferType(member.Value);
                if (!valueType.IsError && !valueType.IsInteger)
                {
                    AddError("VALK2065",
                        $"位标志 '{flagsDecl.Name}' 的成员 '{member.Name}' 值类型不合法：期望整数，实际为 '{valueType}'",
                        member.Span, "位标志成员值必须是整数");
                }
            }

            var memberType = new ValkyrieType(TypeKind.Enum, $"{flagsDecl.Name}.{member.Name}");
            variantTypes.Add(memberType);

            var memberSymbol = new Symbol(member.Name, memberType, SymbolKind.Variable);
            if (!_currentScope.Define(memberSymbol))
            {
                AddWarning("VALK2066",
                    $"位标志成员 '{member.Name}' 与外层作用域符号冲突",
                    member.Span);
            }
        }

        var flagsType = new ValkyrieType(TypeKind.Enum, flagsDecl.Name, variantTypes);
        _typeRegistry[flagsDecl.Name] = flagsType;

        var symbol = new Symbol(flagsDecl.Name, flagsType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2067",
                $"位标志 '{flagsDecl.Name}' 已在当前作用域中定义",
                flagsDecl.Span, "重命名位标志以避免重复定义");
        }
    }

    /// <summary>
    /// 检查联合类型声明：验证变体字段类型，注册联合类型和变体构造器符号
    /// </summary>
    private void CheckUnionDecl(UnionDecl unionDecl)
    {
        var variantNames = new HashSet<string>(StringComparer.Ordinal);
        var variantTypes = new List<ValkyrieType>();

        var unionScope = new Scope(_currentScope);

        foreach (var variant in unionDecl.Variants)
        {
            if (!variantNames.Add(variant.Name))
            {
                AddError("VALK2068",
                    $"联合类型 '{unionDecl.Name}' 中变体 '{variant.Name}' 重复定义",
                    variant.Span, "重命名变体以避免重复定义");
                continue;
            }

            var fieldTypes = new Dictionary<string, ValkyrieType>(StringComparer.Ordinal);
            foreach (var field in variant.Fields)
            {
                var fieldType = ResolveTypeAnnotation(field.FieldType);

                if (fieldTypes.ContainsKey(field.Name))
                {
                    AddError("VALK2069",
                        $"联合变体 '{unionDecl.Name}.{variant.Name}' 中字段 '{field.Name}' 重复定义",
                        field.Span, "重命名字段以避免重复定义");
                }
                else
                {
                    fieldTypes[field.Name] = fieldType;
                }
            }

            var variantType = new ValkyrieType(TypeKind.Class, $"{unionDecl.Name}.{variant.Name}");
            variantTypes.Add(variantType);

            var variantSymbol = new Symbol(variant.Name, variantType, SymbolKind.Class);
            if (!_currentScope.Define(variantSymbol))
            {
                AddWarning("VALK2070",
                    $"联合变体 '{variant.Name}' 与外层作用域符号冲突",
                    variant.Span);
            }
        }

        var unionType = new ValkyrieType(TypeKind.Union, unionDecl.Name, variantTypes);
        _typeRegistry[unionDecl.Name] = unionType;

        var symbol = new Symbol(unionDecl.Name, unionType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2071",
                $"联合类型 '{unionDecl.Name}' 已在当前作用域中定义",
                unionDecl.Span, "重命名联合类型以避免重复定义");
        }
    }

    /// <summary>
    /// 检查 C 风格联合声明：验证变体字段类型，注册 unite 类型
    /// </summary>
    private void CheckUniteDecl(UniteDecl uniteDecl)
    {
        var variantNames = new HashSet<string>(StringComparer.Ordinal);
        var variantTypes = new List<ValkyrieType>();

        foreach (var variant in uniteDecl.Variants)
        {
            if (!variantNames.Add(variant.Name))
            {
                AddError("VALK2072",
                    $"unite '{uniteDecl.Name}' 中变体 '{variant.Name}' 重复定义",
                    variant.Span, "重命名变体以避免重复定义");
                continue;
            }

            var fieldTypes = new Dictionary<string, ValkyrieType>(StringComparer.Ordinal);
            foreach (var field in variant.Fields)
            {
                var fieldType = ResolveTypeAnnotation(field.FieldType);

                if (fieldTypes.ContainsKey(field.Name))
                {
                    AddError("VALK2073",
                        $"unite 变体 '{uniteDecl.Name}.{variant.Name}' 中字段 '{field.Name}' 重复定义",
                        field.Span, "重命名字段以避免重复定义");
                }
                else
                {
                    fieldTypes[field.Name] = fieldType;
                }
            }

            var variantType = new ValkyrieType(TypeKind.Class, $"{uniteDecl.Name}.{variant.Name}");
            variantTypes.Add(variantType);

            var variantSymbol = new Symbol(variant.Name, variantType, SymbolKind.Class);
            if (!_currentScope.Define(variantSymbol))
            {
                AddWarning("VALK2074",
                    $"unite 变体 '{variant.Name}' 与外层作用域符号冲突",
                    variant.Span);
            }
        }

        var uniteType = new ValkyrieType(TypeKind.Union, uniteDecl.Name, variantTypes);
        _typeRegistry[uniteDecl.Name] = uniteType;

        var symbol = new Symbol(uniteDecl.Name, uniteType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2075",
                $"unite '{uniteDecl.Name}' 已在当前作用域中定义",
                uniteDecl.Span, "重命名 unite 以避免重复定义");
        }
    }

    /// <summary>
    /// 检查类型别名声明：验证目标类型存在，注册别名    /// </summary>
    private void CheckTypeAliasDecl(TypeAliasDecl typeAliasDecl)
    {
        var targetType = ResolveTypeAnnotation(typeAliasDecl.TargetType);

        if (targetType.IsError || targetType.Kind == TypeKind.Unknown)
        {
            AddError("VALK2076",
                $"类型别名 '{typeAliasDecl.Name}' 引用了未定义的类型 '{typeAliasDecl.TargetType.Name}'",
                typeAliasDecl.Span, "确保目标类型已定义");
        }

        _typeRegistry[typeAliasDecl.Name] = targetType;

        var symbol = new Symbol(typeAliasDecl.Name, targetType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2077",
                $"类型别名 '{typeAliasDecl.Name}' 已在当前作用域中定义",
                typeAliasDecl.Span, "重命名类型别名以避免重复定义");
        }
    }

    /// <summary>
    /// 检查命名空间声明：创建子作用域，检查命名空间内声明
    /// </summary>
    private void CheckNamespaceDecl(NamespaceDecl namespaceDecl)
    {
        var nsScope = new Scope(_currentScope);
        var prevScope = _currentScope;
        _currentScope = nsScope;

        foreach (var decl in namespaceDecl.Declarations)
        {
            CheckDeclaration(decl);
        }

        _currentScope = prevScope;

        var nsType = new ValkyrieType(TypeKind.Unknown, namespaceDecl.Name);
        _typeRegistry[namespaceDecl.Name] = nsType;

        var symbol = new Symbol(namespaceDecl.Name, nsType, SymbolKind.Class);
        if (!_globalScope.Define(symbol))
        {
            AddError("VALK2078",
                $"命名空间 '{namespaceDecl.Name}' 已在全局作用域中定义",
                namespaceDecl.Span, "重命名命名空间以避免重复定义");
        }
    }

    #endregion

    #region 着色器声明检查
    private void CheckShaderDecl(ShaderDecl shaderDecl)
    {
        var shaderScope = new Scope(_currentScope);

        foreach (var stage in shaderDecl.Stages)
        {
            CheckShaderStageDecl(stage, shaderScope);
        }

        var shaderType = new ValkyrieType(TypeKind.Shader, shaderDecl.Name);
        _typeRegistry[shaderDecl.Name] = shaderType;

        var symbol = new Symbol(shaderDecl.Name, shaderType, SymbolKind.Variable);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2030", $"着色器 '{shaderDecl.Name}' 已在当前作用域中定义", shaderDecl.Span, "重命名着色器以避免重复定义");
        }
    }

    private void CheckShaderStageDecl(ShaderStageDecl stage, Scope shaderScope)
    {
        var stageScope = new Scope(shaderScope);

        foreach (var bodyNode in stage.Body)
        {
            switch (bodyNode)
            {
                case UniformDecl uniform:
                    CheckUniformDeclInScope(uniform, stageScope);
                    break;
                case VaryingDecl varying:
                    CheckVaryingDeclInScope(varying, stageScope);
                    break;
                case ConstantBufferDecl cbuffer:
                    CheckConstantBufferDeclInScope(cbuffer, stageScope);
                    break;
                case TextureDecl texture:
                    CheckTextureDeclInScope(texture, stageScope);
                    break;
                case SamplerDecl sampler:
                    CheckSamplerDeclInScope(sampler, stageScope);
                    break;
                case ShaderAttributeDecl attr:
                    CheckShaderAttributeDeclInScope(attr, stageScope);
                    break;
                case FunctionDecl func:
                    CheckFunctionDeclInScope(func, stageScope);
                    break;
                case VariableDecl varDecl:
                    CheckVariableDecl(varDecl);
                    break;
                case StructureDecl structDecl:
                    CheckStructDecl(structDecl);
                    break;
                default:
                    CheckDeclaration(bodyNode);
                    break;
            }
        }
    }

    private void CheckStructDecl(StructureDecl structDecl)
    {
        var fields = new Dictionary<string, ValkyrieType>(StringComparer.Ordinal);

        foreach (var field in structDecl.Fields)
        {
            var fieldType = ResolveTypeAnnotation(field.FieldType);

            if (fields.ContainsKey(field.Name))
            {
                AddError("VALK2031", $"结构体'{structDecl.Name}' 中字段 '{field.Name}' 重复定义", field.Span, "重命名字段以避免重复定义");
            }
            else
            {
                fields[field.Name] = fieldType;
            }
        }

        var structType = new ValkyrieType(TypeKind.Struct, structDecl.Name);
        _typeRegistry[structDecl.Name] = structType;

        var symbol = new Symbol(structDecl.Name, structType, SymbolKind.Variable);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2032", $"结构体'{structDecl.Name}' 已在当前作用域中定义", structDecl.Span, "重命名结构体以避免重复定义");
        }
    }

    private void CheckClassDecl(ClassDecl classDecl)
    {
        var outerTypeNames = new HashSet<string>(StringComparer.Ordinal);
        var fieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var inheritance in classDecl.Inheritances)
        {
            var outerTypeName = inheritance.BaseType.Name;

            if (!outerTypeNames.Add(outerTypeName))
            {
                AddError("VALK3001",
                    $"类 '{classDecl.Name}' 中基类类型 '{outerTypeName}' 重复，请使用具名继承区分",
                    inheritance.Span, "移除重复的基类，或使用接口代替多继承");
            }

            var effectiveFieldName = inheritance.EffectiveFieldName;

            if (fieldNames.Contains(effectiveFieldName))
            {
                AddError("VALK3003",
                    $"类 '{classDecl.Name}' 中继承字段名 '{effectiveFieldName}' 冲突",
                    inheritance.Span, "使用 override 关键字标记方法覆写");
            }

            fieldNames.Add(effectiveFieldName);
        }

        foreach (var field in classDecl.Fields)
        {
            if (fieldNames.Contains(field.Name))
            {
                AddError("VALK3002",
                    $"类 '{classDecl.Name}' 中字段 '{field.Name}' 与继承字段冲突",
                    field.Span, "重命名字段以避免与基类冲突");
            }

            fieldNames.Add(field.Name);
            ResolveTypeAnnotation(field.FieldType);
        }

        foreach (var func in classDecl.Functions)
        {
            if (func.ReturnType is not null)
            {
                ResolveTypeAnnotation(func.ReturnType);
            }

            foreach (var param in func.Parameters)
            {
                ResolveTypeAnnotation(param.ParamType);
            }
        }

        var classType = new ValkyrieType(TypeKind.Class, classDecl.Name);
        _typeRegistry[classDecl.Name] = classType;

        var symbol = new Symbol(classDecl.Name, classType, SymbolKind.Class);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2033", $"类 '{classDecl.Name}' 已在当前作用域中定义", classDecl.Span, "重命名以避免重复定义");
        }
    }

    private void CheckUniformDecl(UniformDecl uniformDecl)
    {
        CheckUniformDeclInScope(uniformDecl, _currentScope);
    }

    private void CheckUniformDeclInScope(UniformDecl uniformDecl, Scope scope)
    {
        var uniformType = ResolveTypeAnnotation(uniformDecl.UniformType);

        var symbol = new Symbol(uniformDecl.Name, uniformType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2033", $"uniform 变量 '{uniformDecl.Name}' 已在当前作用域中定义", uniformDecl.Span, "重命名以避免重复定义");
        }
    }

    private void CheckVaryingDecl(VaryingDecl varyingDecl)
    {
        CheckVaryingDeclInScope(varyingDecl, _currentScope);
    }

    private void CheckVaryingDeclInScope(VaryingDecl varyingDecl, Scope scope)
    {
        var varyingType = ResolveTypeAnnotation(varyingDecl.VaryingType);

        var symbol = new Symbol(varyingDecl.Name, varyingType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2034", $"varying 变量 '{varyingDecl.Name}' 已在当前作用域中定义", varyingDecl.Span, "重命名 varying 变量以避免重复定义");
        }
    }

    private void CheckConstantBufferDecl(ConstantBufferDecl cbufferDecl)
    {
        CheckConstantBufferDeclInScope(cbufferDecl, _currentScope);
    }

    private void CheckConstantBufferDeclInScope(ConstantBufferDecl cbufferDecl, Scope scope)
    {
        var fields = new Dictionary<string, ValkyrieType>(StringComparer.Ordinal);

        foreach (var field in cbufferDecl.Fields)
        {
            var fieldType = ResolveTypeAnnotation(field.FieldType);

            if (fields.ContainsKey(field.Name))
            {
                AddError("VALK2035", $"cbuffer '{cbufferDecl.Name}' 中字段 '{field.Name}' 重复定义", field.Span, "重命名字段以避免重复定义");
            }
            else
            {
                fields[field.Name] = fieldType;
            }
        }

        var cbufferType = new ValkyrieType(TypeKind.Struct, cbufferDecl.Name);
        _typeRegistry[cbufferDecl.Name] = cbufferType;

        var symbol = new Symbol(cbufferDecl.Name, cbufferType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2036", $"cbuffer '{cbufferDecl.Name}' 已在当前作用域中定义", cbufferDecl.Span, "重命名 cbuffer 以避免重复定义");
        }
    }

    private void CheckTextureDecl(TextureDecl textureDecl)
    {
        CheckTextureDeclInScope(textureDecl, _currentScope);
    }

    private void CheckTextureDeclInScope(TextureDecl textureDecl, Scope scope)
    {
        var textureType = ResolveTypeAnnotation(textureDecl.TextureType);

        var symbol = new Symbol(textureDecl.Name, textureType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2037", $"纹理 '{textureDecl.Name}' 已在当前作用域中定义", textureDecl.Span, "重命名纹理以避免重复定义");
        }
    }

    private void CheckSamplerDecl(SamplerDecl samplerDecl)
    {
        CheckSamplerDeclInScope(samplerDecl, _currentScope);
    }

    private void CheckSamplerDeclInScope(SamplerDecl samplerDecl, Scope scope)
    {
        var samplerType = new ValkyrieType(TypeKind.Unknown, "sampler");

        var symbol = new Symbol(samplerDecl.Name, samplerType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2038", $"采样器 '{samplerDecl.Name}' 已在当前作用域中定义", samplerDecl.Span, "重命名采样器以避免重复定义");
        }
    }

    private void CheckUniformBindingDecl(UniformBindingDecl uniformBindingDecl)
    {
        var bindingType = ResolveTypeAnnotation(uniformBindingDecl.BindingType);

        var symbol = new Symbol(uniformBindingDecl.Name, bindingType, SymbolKind.Variable);
        if (!_currentScope.Define(symbol))
        {
            AddError("VALK2039", $"uniform 绑定 '{uniformBindingDecl.Name}' 已在当前作用域中定义", uniformBindingDecl.Span, "重命名 uniform 绑定以避免重复定义");
        }
    }

    private void CheckShaderAttributeDecl(ShaderAttributeDecl shaderAttrDecl)
    {
        CheckShaderAttributeDeclInScope(shaderAttrDecl, _currentScope);
    }

    private void CheckShaderAttributeDeclInScope(ShaderAttributeDecl shaderAttrDecl, Scope scope)
    {
        var attrType = ResolveTypeAnnotation(shaderAttrDecl.AttrType);

        var symbol = new Symbol(shaderAttrDecl.Name, attrType, SymbolKind.Variable);
        if (!scope.Define(symbol))
        {
            AddError("VALK2040", $"着色器属性 '{shaderAttrDecl.Name}' 已在当前作用域中定义", shaderAttrDecl.Span, "重命名着色器属性以避免重复定义");
        }
    }

    private ValkyrieType InferSwizzleType(SwizzleExpr swizzle)
    {
        var targetType = InferType(swizzle.Target);

        if (targetType.IsError)
        {
            return ValkyrieType.Error;
        }

        var componentCount = swizzle.Components.Length;

        if (!IsValidSwizzle(targetType, swizzle.Components))
        {
            AddError("VALK2041",
                $"无效的 swizzle 操作 '{swizzle.Components}' 不能应用于类型 '{targetType}'",
                swizzle.Span, "确保枚举成员类型已定义");
            return ValkyrieType.Error;
        }

        return componentCount switch
        {
            1 => GetSwizzleScalarType(targetType),
            2 => GetSwizzleVecType(targetType, 2),
            3 => GetSwizzleVecType(targetType, 3),
            4 => GetSwizzleVecType(targetType, 4),
            _ => ValkyrieType.Error
        };
    }

    private static bool IsValidSwizzle(ValkyrieType targetType, string components)
    {
        var validChars = targetType.Name switch
        {
            "vec2" => "xy",
            "vec3" => "xyz",
            "vec4" => "xyzw",
            "ivec2" => "xy",
            "ivec3" => "xyz",
            "ivec4" => "xyzw",
            "uvec2" => "xy",
            "uvec3" => "xyz",
            "uvec4" => "xyzw",
            _ => ""
        };

        if (string.IsNullOrEmpty(validChars))
        {
            return false;
        }

        foreach (var c in components)
        {
            if (!validChars.Contains(c))
            {
                return false;
            }
        }

        return true;
    }

    private static ValkyrieType GetSwizzleScalarType(ValkyrieType targetType)
    {
        return targetType.Name switch
        {
            "vec2" or "vec3" or "vec4" => ValkyrieType.F32,
            "ivec2" or "ivec3" or "ivec4" => ValkyrieType.I32,
            "uvec2" or "uvec3" or "uvec4" => ValkyrieType.U32,
            _ => ValkyrieType.Error
        };
    }

    private static ValkyrieType GetSwizzleVecType(ValkyrieType targetType, int componentCount)
    {
        var isFloat = targetType.Name.StartsWith("vec") && !targetType.Name.StartsWith("ivec") && !targetType.Name.StartsWith("uvec");
        var isInt = targetType.Name.StartsWith("ivec");
        var isUint = targetType.Name.StartsWith("uvec");

        if (isFloat)
        {
            return componentCount switch
            {
                2 => ValkyrieType.Vec2,
                3 => ValkyrieType.Vec3,
                4 => ValkyrieType.Vec4,
                _ => ValkyrieType.Error
            };
        }

        if (isInt)
        {
            return componentCount switch
            {
                2 => ValkyrieType.IVec2,
                3 => ValkyrieType.IVec3,
                4 => ValkyrieType.IVec4,
                _ => ValkyrieType.Error
            };
        }

        if (isUint)
        {
            return componentCount switch
            {
                2 => ValkyrieType.UVec2,
                3 => ValkyrieType.UVec3,
                4 => ValkyrieType.UVec4,
                _ => ValkyrieType.Error
            };
        }

        return ValkyrieType.Error;
    }

    #endregion
}