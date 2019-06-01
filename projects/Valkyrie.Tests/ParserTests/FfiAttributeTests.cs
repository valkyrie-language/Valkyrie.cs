using Oak.Diagnostics;
using Oak.Valkyrie;
using Oak.Valkyrie.AST;
using Oak.Valkyrie.Lexer;
using Oak.Valkyrie.Parser;
using Valkyrie.Interpreter.Converter;
using Xunit;

namespace Valkyrie.Tests.ParserTests;

public class FfiAttributeTests
{
    private readonly DiagnosticSink _diagnostics = new();
    private readonly ValkyrieLexer _lexer;
    private readonly ValkyrieParser _parser;

    public FfiAttributeTests()
    {
        _lexer = new ValkyrieLexer(_diagnostics);
        _parser = new ValkyrieParser(ValkyrieLanguage.Standard, _diagnostics);
    }

    private CompilationUnit ParseSource(string source)
    {
        _diagnostics.Clear();
        var tokens = _lexer.Tokenize(source);
        var result = _parser.Parse(tokens);
        return Assert.IsType<CompilationUnit>(result);
    }

    [Fact]
    public void CAttribute_ShouldParse()
    {
        var source = """
                     [c("libc", "write")]
                     micro posix_write(fd: i32, buf: c_str, len: i32): i32
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("posix_write", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("c", func.Attributes[0].Name);
        Assert.Equal(2, func.Attributes[0].Arguments.Count);
        Assert.Equal("", func.Attributes[0].Arguments[0].Key);
        Assert.Equal("libc", func.Attributes[0].Arguments[0].Value);
        Assert.Equal("", func.Attributes[0].Arguments[1].Key);
        Assert.Equal("write", func.Attributes[0].Arguments[1].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void ComAttribute_ShouldParse()
    {
        var source = """
                     [com("IUnknown", "Release")]
                     micro com_release(this: i32): i32
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("com_release", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("com", func.Attributes[0].Name);
        Assert.Equal(2, func.Attributes[0].Arguments.Count);
        Assert.Equal("IUnknown", func.Attributes[0].Arguments[0].Value);
        Assert.Equal("Release", func.Attributes[0].Arguments[1].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void SyscallAttribute_ShouldParse()
    {
        var source = """
                     [syscall(1)]
                     micro sys_write(fd: i32, buf: c_str, len: i32): i64
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("sys_write", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("syscall", func.Attributes[0].Name);
        Assert.Single(func.Attributes[0].Arguments);
        Assert.Equal("1", func.Attributes[0].Arguments[0].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void ClrAttribute_ShouldParse()
    {
        var source = """
                     [clr("System.Console", "WriteLine")]
                     micro clr_console_write_line(value: string)
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("clr_console_write_line", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("clr", func.Attributes[0].Name);
        Assert.Equal(2, func.Attributes[0].Arguments.Count);
        Assert.Equal("System.Console", func.Attributes[0].Arguments[0].Value);
        Assert.Equal("WriteLine", func.Attributes[0].Arguments[1].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void DlrAttribute_ShouldParse()
    {
        var source = """
                     [dlr]
                     micro dlr_new(type_name: string): i32
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("dlr_new", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("dlr", func.Attributes[0].Name);
        Assert.Empty(func.Attributes[0].Arguments);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void JvmAttribute_ShouldParse()
    {
        var source = """
                     [jvm("java/lang/Math", "sin")]
                     micro jvm_math_sin(x: f64): f64
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("jvm_math_sin", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("jvm", func.Attributes[0].Name);
        Assert.Equal(2, func.Attributes[0].Arguments.Count);
        Assert.Equal("java/lang/Math", func.Attributes[0].Arguments[0].Value);
        Assert.Equal("sin", func.Attributes[0].Arguments[1].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void ImportAttribute_ShouldParse()
    {
        var source = """
                     [import("std.math")]
                     micro math_add(x: f64, y: f64): f64
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("math_add", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("import", func.Attributes[0].Name);
        Assert.Single(func.Attributes[0].Arguments);
        Assert.Equal("std.math", func.Attributes[0].Arguments[0].Value);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void PureAttribute_ShouldParse()
    {
        var source = """
                     [pure]
                     micro add(a: i32, b: i32): i32 { return a + b }
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("add", func.Name);
        Assert.Single(func.Attributes);
        Assert.Equal("pure", func.Attributes[0].Name);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void PureAttribute_ShouldCombineWithOtherAttributes()
    {
        var source = """
                     [clr("System.Math", "Sin"), pure]
                     micro clr_math_sin(x: f64): f64
                     """;
        
        var unit = ParseSource(source);
        Assert.Single(unit.Declarations);
        
        var func = Assert.IsType<FunctionDecl>(unit.Declarations[0]);
        Assert.Equal("clr_math_sin", func.Name);
        Assert.Equal(2, func.Attributes.Count);
        Assert.Equal("clr", func.Attributes[0].Name);
        Assert.Equal("pure", func.Attributes[1].Name);
        
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void CAttribute_ShouldConvertToIkunImport()
    {
        var source = """
                     [c("libc", "write")]
                     micro posix_write(fd: i32, buf: c_str, len: i32): i32
                     """;
        
        var unit = ParseSource(source);
        var converter = new AstToIkunConverter();
        var result = converter.Convert(unit, "ffi_test");
        
        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void AllFfiAttributes_ShouldParseWithoutErrors()
    {
        var source = """
                     [c("libc", "write")]
                     micro posix_write(fd: i32, buf: c_str, len: i32): i32

                     [com("IUnknown", "Release")]
                     micro com_release(this: i32): i32

                     [syscall(1)]
                     micro sys_write(fd: i32, buf: c_str, len: i32): i64

                     [clr("System.Console", "WriteLine")]
                     micro clr_console_write_line(value: string)

                     [dlr]
                     micro dlr_new(type_name: string): i32

                     [jvm("java/lang/Math", "sin")]
                     micro jvm_math_sin(x: f64): f64

                     [import("std.math")]
                     micro math_add(x: f64, y: f64): f64

                     [pure]
                     micro add(a: i32, b: i32): i32 { return a + b }
                     """;
        
        var unit = ParseSource(source);
        Assert.Equal(8, unit.Declarations.Count);
        Assert.Empty(_diagnostics.Errors);
    }

    [Fact]
    public void AllFfiAttributes_ShouldConvertToIkun()
    {
        var source = """
                     [c("libc", "write")]
                     micro posix_write(fd: i32, buf: c_str, len: i32): i32

                     [com("IUnknown", "Release")]
                     micro com_release(this: i32): i32

                     [syscall(1)]
                     micro sys_write(fd: i32, buf: c_str, len: i32): i64

                     [clr("System.Console", "WriteLine")]
                     micro clr_console_write_line(value: string)

                     [dlr]
                     micro dlr_new(type_name: string): i32

                     [jvm("java/lang/Math", "sin")]
                     micro jvm_math_sin(x: f64): f64

                     [import("std.math")]
                     micro math_add(x: f64, y: f64): f64

                     [pure]
                     micro add(a: i32, b: i32): i32 { return a + b }
                     """;
        
        var unit = ParseSource(source);
        var converter = new AstToIkunConverter();
        var result = converter.Convert(unit, "ffi_test");
        
        Assert.NotNull(result);
        Assert.NotNull(result.EGraph);
        Assert.Empty(_diagnostics.Errors);
    }
}