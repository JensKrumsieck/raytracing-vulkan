uint gState = 78213298; // global state

uint pcg_hash() {
    uint state = gState * uint(747796405) + uint(2891336453);
    uint word = ((state >> ((state >> 28) + 4)) ^ state) * uint(277803737);
    return (word >> 22) ^ word;
}

float hash1() {
    gState = pcg_hash();
    return float(gState) / float(uint(0xffffffff));
}

vec3 hash3() {
    return vec3(hash1(), hash1(), hash1());
}

float hash1(float min, float max) {
    return hash1() * (max - min) + min;
}

vec3 hash3(float min, float max) {
    return hash3() * (max - min) + min;
}

vec3 inUnitSphere(){
    while(true) {
        vec3 p = hash3(-1, 1);
        if(dot(p,p) < 1) return p;
    }
}

vec3 randomUnitVector() {
    return normalize(inUnitSphere());
}