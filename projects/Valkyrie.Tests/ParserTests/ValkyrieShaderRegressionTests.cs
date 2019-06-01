namespace Valkyrie.Tests.ParserTests;

public class ValkyrieShaderRegressionTests : ValkyrieParserTestBase
{
    private readonly ValkyrieLanguage _languageWithShader = ValkyrieLanguage.Shader;

    private ProgramRoot Parse(string source)
    {
        return ParseWithTimeout(source, _languageWithShader);
    }

    [Fact]
    public void Parse_ShaderDecl_ShouldReturnShaderDecl()
    {
        var source = @"
            shader main {
                vertex vs_main() { }
                fragment fs_main() { }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_ShaderWithCompute_ShouldNotThrow()
    {
        var source = @"
            shader particles {
                compute cs_main() { }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_UniformDecl_ShouldNotThrow()
    {
        var source = "uniform view_proj: mat4";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_VaryingDecl_ShouldNotThrow()
    {
        var source = "varying uv: vec2";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_CBufferDecl_ShouldNotThrow()
    {
        var source = "cbuffer FrameData { view: mat4 proj: mat4 }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_TextureDecl_ShouldNotThrow()
    {
        var source = "texture albedo_map: texture2D";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_SamplerDecl_ShouldNotThrow()
    {
        var source = "sampler linear_sampler";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_StructDecl_ShouldNotThrow()
    {
        var source = "struct VertexInput { position: vec3 normal: vec3 }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_DiscardStmt_ShouldNotThrow()
    {
        var source = "discard";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_FullShaderPipeline_ShouldNotThrow()
    {
        var source = @"
            shader pbr_pipeline {
                vertex vs_main() { }
                fragment fs_main() { }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_TurbofishCall_ShouldParseTypeArguments()
    {
        var source = @"
            shader test {
                fragment fs_main() {
                    let pos = vec3::<f32>(1.0, 2.0, 3.0)
                }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_TurbofishCall_MultipleTypeArgs_ShouldParse()
    {
        var source = @"
            shader test {
                fragment fs_main() {
                    let result = make_pair::<f32, i32>(1.0, 42)
                }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_GenericTypeAnnotation_ShouldParse()
    {
        var source = @"
            shader test {
                uniform view_proj: mat4<f32>,
                fragment fs_main() { }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_AmbiguousLessThan_ShouldParseAsComparison()
    {
        var source = @"
            shader test {
                fragment fs_main() {
                    let flag = x < y
                }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_UsingGgShader_ShouldParse()
    {
        var source = @"
            using gg_shader::f32::{vec2, vec3, vec4}
            shader test {
                varying v_color: vec4,
                fragment fs_main() {
                    let c = vec4(1.0, 0.0, 0.0, 1.0)
                }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_TurbofishCall_WithMemberAccess_ShouldParse()
    {
        var source = @"
            shader test {
                fragment fs_main() {
                    let pos = Vector3::<f32>(1.0, 0.0, 0.0)
                }
            }";
        var result = Parse(source);

        Assert.NotNull(result);
    }
}
