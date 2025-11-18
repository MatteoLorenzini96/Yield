Shader "Custom/MaskHole"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Stencil
            {
                Ref 1       // valore scritto nello stencil
                Comp always // sempre scrive
                Pass replace
            }

            ColorMask 0   // non scrive colore
            ZWrite Off    // non modifica depth
        }
    }
}
