float gamma_correction(float linearComponent){
    return pow(linearComponent, 1.0/2.2);
}

vec3 gamma_correction(vec3 linearColor)
{
    return  vec3(
                gamma_correction(linearColor.x),
                gamma_correction(linearColor.y),
                gamma_correction(linearColor.z));
}
