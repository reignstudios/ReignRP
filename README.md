# About
ReignRP is a Forward only Unity3D Render-Pipeline focused on VR and low-end GPU performance while maintaining visual appeal.<br><br>
URP seems to have performance overhead BuiltInRP does not causing regression for performance constrained projects. There are also other complexities in URP in its ShaderGraph, lighting override or post-processing pipelines that are more time consuming or missing fundumental vertex buffer streams causing technical challanges.

## WIP
* Standard HLSL shader system with custom override features
* VR support
* PBR Lit features
* Lightmap features
* GI features (TBD)

### Why it can be faster
* Reduced culling options
* Simpler shader options
* Shader complexity reduction options
* Simpler shadow options