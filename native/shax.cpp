#include <GLES3/gl3.h>
#include <android/log.h>
#include <array>
#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <vector>

#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, "ShaxNative", __VA_ARGS__)
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, "ShaxNative", __VA_ARGS__)
namespace {
GLuint sceneProgram = 0, pbrProgram = 0, skyProgram = 0, sceneVbo = 0, cubeVbo = 0, environmentCube = 0;
GLint sceneMvp = -1, pbrMvp = -1, pbrModel = -1, pbrEnvironment = -1, pbrCamera = -1, pbrLight = -1, pbrRoughness = -1, pbrMetallic = -1;
float cameraX = 0.0f, cameraY = 1.6f, cameraZ = 4.0f, materialRoughness = 0.28f, materialMetallic = 0.08f;
GLint skyTime = -1;
GLsizei gridCount = 0, cubeCount = 0;
std::vector<float> sceneVertices, cubeVertices;
constexpr float PI = 3.14159265358979323846f;

GLuint compile(GLenum type, const char* source) {
    GLuint shader = glCreateShader(type);
    glShaderSource(shader, 1, &source, nullptr);
    glCompileShader(shader);
    GLint ok = GL_FALSE;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &ok);
    if (ok != GL_TRUE) {
        GLint length = 0; glGetShaderiv(shader, GL_INFO_LOG_LENGTH, &length);
        std::vector<char> log(static_cast<size_t>(length > 1 ? length : 2));
        glGetShaderInfoLog(shader, length, nullptr, log.data());
        LOGE("Shader compile failed: %s", log.data());
        glDeleteShader(shader); return 0;
    }
    return shader;
}
GLuint linkProgram(const char* vsSource, const char* fsSource) {
    GLuint vs = compile(GL_VERTEX_SHADER, vsSource), fs = compile(GL_FRAGMENT_SHADER, fsSource);
    if (!vs || !fs) { if (vs) glDeleteShader(vs); if (fs) glDeleteShader(fs); return 0; }
    GLuint p = glCreateProgram();
    glAttachShader(p, vs); glAttachShader(p, fs); glLinkProgram(p);
    glDeleteShader(vs); glDeleteShader(fs);
    GLint ok = GL_FALSE; glGetProgramiv(p, GL_LINK_STATUS, &ok);
    if (ok != GL_TRUE) {
        GLint length = 0; glGetProgramiv(p, GL_INFO_LOG_LENGTH, &length);
        std::vector<char> log(static_cast<size_t>(length > 1 ? length : 2));
        glGetProgramInfoLog(p, length, nullptr, log.data());
        LOGE("Program link failed: %s", log.data()); glDeleteProgram(p); return 0;
    }
    return p;
}
void vertex(std::vector<float>& out, float x, float y, float z, float r, float g, float b) {
    out.insert(out.end(), {x,y,z,r,g,b});
}
void line(std::vector<float>& out, float ax,float ay,float az,float bx,float by,float bz,
          float r,float g,float b) {
    vertex(out,ax,ay,az,r,g,b); vertex(out,bx,by,bz,r,g,b);
}
void triangle(std::vector<float>& out, const std::array<float,3>& a,
              const std::array<float,3>& b, const std::array<float,3>& c,
              float r,float g,float blue) {
    vertex(out,a[0],a[1],a[2],r,g,blue); vertex(out,b[0],b[1],b[2],r,g,blue);
    vertex(out,c[0],c[1],c[2],r,g,blue);
}
void face(std::vector<float>& out, std::array<float,3> a, std::array<float,3> b,
          std::array<float,3> c, std::array<float,3> d, float r,float g,float blue) {
    triangle(out,a,b,c,r,g,blue); triangle(out,a,c,d,r,g,blue);
}
void buildGeometry() {
    sceneVertices.clear();
    for (int i=-20;i<=20;++i) {
        float tint = (i % 5 == 0) ? 0.32f : 0.15f;
        line(sceneVertices, (float)i,0,-20,(float)i,0,20, tint,tint*1.12f,tint*1.3f);
        line(sceneVertices, -20,0,(float)i,20,0,(float)i, tint,tint*1.12f,tint*1.3f);
    }
    gridCount = static_cast<GLsizei>(sceneVertices.size()/6);
    line(sceneVertices,-20,0.006f,0,20,0.006f,0,0.96f,0.22f,0.25f);
    line(sceneVertices,0,0.008f,-20,0,0.008f,20,0.2f,0.48f,1.0f);
    line(sceneVertices,0,0,0,0,4,0,0.25f,0.9f,0.45f);
    cubeCount = 36;
    const std::array<float,3> a{-0.5f,-0.5f,-0.5f}, b{0.5f,-0.5f,-0.5f};
    const std::array<float,3> c{0.5f,0.5f,-0.5f}, d{-0.5f,0.5f,-0.5f};
    const std::array<float,3> e{-0.5f,-0.5f,0.5f}, f{0.5f,-0.5f,0.5f};
    const std::array<float,3> g{0.5f,0.5f,0.5f}, h{-0.5f,0.5f,0.5f};
    face(sceneVertices,a,b,c,d,0.12f,0.55f,0.95f);
    face(sceneVertices,f,e,h,g,0.08f,0.36f,0.72f);
    face(sceneVertices,e,a,d,h,0.1f,0.45f,0.82f);
    face(sceneVertices,b,f,g,c,0.18f,0.68f,1.0f);
    face(sceneVertices,d,c,g,h,0.34f,0.78f,1.0f);
    face(sceneVertices,e,f,b,a,0.06f,0.28f,0.55f);
}
constexpr const char* sceneVs = R"(#version 300 es
layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aColor;
uniform mat4 uMvp;
out vec3 vColor;
void main(){ vColor=aColor; gl_Position=uMvp*vec4(aPosition,1.0); })";
constexpr const char* sceneFs = R"(#version 300 es
precision mediump float;
in vec3 vColor; out vec4 fragColor;
void main(){ fragColor=vec4(vColor,1.0); })";

constexpr const char* pbrVs = R"(#version 300 es
layout(location=0) in vec3 aPosition; layout(location=1) in vec3 aNormal; layout(location=2) in vec3 aAlbedo;
uniform mat4 uMvp; uniform mat4 uModel; out vec3 vWorld; out vec3 vNormal; out vec3 vAlbedo;
void main(){vec4 w=uModel*vec4(aPosition,1.0);vWorld=w.xyz;vNormal=normalize(mat3(uModel)*aNormal);vAlbedo=aAlbedo;gl_Position=uMvp*vec4(aPosition,1.0);})";
constexpr const char* pbrFs = R"(#version 300 es
precision highp float; in vec3 vWorld; in vec3 vNormal; in vec3 vAlbedo; out vec4 fragColor;
uniform samplerCube uEnvironment; uniform vec3 uCamera; uniform vec3 uLightDirection; uniform float uRoughness; uniform float uMetallic;
const float PI=3.14159265359;
vec3 fresnel(float c,vec3 f0){return f0+(1.0-f0)*pow(clamp(1.0-c,0.0,1.0),5.0);}
float distributionGGX(vec3 N,vec3 H,float r){float a=r*r,a2=a*a,n=max(dot(N,H),0.0),d=n*n*(a2-1.0)+1.0;return a2/max(PI*d*d,0.0001);}
float geometrySmith(float n,float r){float k=(r+1.0)*(r+1.0)/8.0;return n/(n*(1.0-k)+k);}
void main(){vec3 N=normalize(vNormal),V=normalize(uCamera-vWorld),L=normalize(-uLightDirection),H=normalize(V+L);
float roughness=clamp(uRoughness,0.04,1.0),metallic=clamp(uMetallic,0.0,1.0),nv=max(dot(N,V),0.001),nl=max(dot(N,L),0.0);
vec3 F0=mix(vec3(0.04),vAlbedo,metallic),F=fresnel(max(dot(H,V),0.0),F0);
float D=distributionGGX(N,H,roughness),G=geometrySmith(nv,roughness)*geometrySmith(nl,roughness);
vec3 spec=(D*G*F)/max(4.0*nv*nl,0.001),kd=(1.0-F)*(1.0-metallic);
vec3 direct=(kd*vAlbedo/PI+spec)*vec3(3.2,3.05,2.8)*nl;
vec3 diffuseIBL=texture(uEnvironment,N).rgb*vAlbedo;
vec3 specularIBL=textureLod(uEnvironment,reflect(-V,N),roughness*4.0).rgb*F;
vec3 color=direct+diffuseIBL*0.42+specularIBL*0.72;color=color/(color+1.0);color=pow(color,vec3(1.0/2.2));fragColor=vec4(color,1.0);})";
constexpr const char* skyVs = R"(#version 300 es
out vec2 vUv;
void main(){ vec2 p=vec2((gl_VertexID<<1)&2,gl_VertexID&2); vUv=p; gl_Position=vec4(p*2.0-1.0,1.0,1.0); })";
constexpr const char* skyFs = R"(#version 300 es
precision highp float; in vec2 vUv; uniform float uTime; out vec4 fragColor;
float hash3(vec3 p){p=fract(p*0.1031);p+=dot(p,p.yxz+33.33);return fract((p.x+p.y)*p.z);}
float noise3(vec3 p){vec3 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);
 float a=hash3(i),b=hash3(i+vec3(1,0,0)),c=hash3(i+vec3(0,1,0)),d=hash3(i+vec3(1,1,0));
 float e=hash3(i+vec3(0,0,1)),f1=hash3(i+vec3(1,0,1)),g=hash3(i+vec3(0,1,1)),h=hash3(i+vec3(1,1,1));
 return mix(mix(mix(a,b,f.x),mix(c,d,f.x),f.y),mix(mix(e,f1,f.x),mix(g,h,f.x),f.y),f.z);}
void main(){
 float h=clamp(vUv.y,0.0,1.0);vec2 p=(vUv-0.5)*vec2(2.4,1.2);
 vec3 ray=normalize(vec3(p.x,h*1.35-0.18,1.0));
 vec3 horizon=vec3(0.69,0.82,0.98),zenith=vec3(0.018,0.055,0.19);
 vec3 sky=mix(horizon,zenith,pow(h,0.78));
 sky+=vec3(0.16,0.25,0.36)*exp(-pow((h-0.16)*7.0,2.0));
 float sun=pow(max(dot(ray,normalize(vec3(-0.35,0.55,0.75))),0.0),360.0);sky+=vec3(1.0,0.84,0.58)*sun*2.4;
 // Fixed 12-step bounded ray march approximates participating-media clouds on ES 3.0/3.2.
 vec3 accumulated=vec3(0.0);float transmittance=1.0;
 for(int i=0;i<12;i++){float t=(float(i)+0.5)/12.0;
  vec3 q=ray*(2.0+t*7.0)+vec3(0.0,uTime*0.012,0.0);
  float cloudBand=exp(-pow((h-0.36)*5.0,2.0));
  float density=smoothstep(0.58,0.82,noise3(q*2.0))*cloudBand*0.085;
  float alpha=1.0-exp(-density);
  accumulated+=transmittance*alpha*mix(vec3(0.23,0.34,0.52),vec3(0.92,0.95,1.0),max(ray.y,0.0));
  transmittance*=1.0-alpha;
 }
 sky=sky*transmittance+accumulated;fragColor=vec4(sky,1.0);
})";

void buildEnvironment() {
    if(!environmentCube) glGenTextures(1,&environmentCube);
    glBindTexture(GL_TEXTURE_CUBE_MAP,environmentCube);
    constexpr int size=32; std::vector<std::uint8_t> pixels(size*size*3);
    for(int face=0;face<6;face++) {
        for(int y=0;y<size;y++) for(int x=0;x<size;x++) {
            float u=2.0f*(x+0.5f)/size-1.0f, v=2.0f*(y+0.5f)/size-1.0f;
            float dx=0.0f,dy=0.0f,dz=1.0f;
            switch(face){case 0:dx=1.0f;dy=-v;dz=-u;break;case 1:dx=-1.0f;dy=-v;dz=u;break;
              case 2:dx=u;dy=1.0f;dz=v;break;case 3:dx=u;dy=-1.0f;dz=-v;break;
              case 4:dx=u;dy=-v;dz=1.0f;break;default:dx=-u;dy=-v;dz=-1.0f;break;}
            float length=std::sqrt(dx*dx+dy*dy+dz*dz); dy/=length;
            float h=std::clamp(0.5f-dy*0.5f,0.0f,1.0f);
            size_t at=static_cast<size_t>((y*size+x)*3);
            pixels[at]=static_cast<std::uint8_t>((0.69f*(1-h)+0.018f*h)*255);
            pixels[at+1]=static_cast<std::uint8_t>((0.82f*(1-h)+0.055f*h)*255);
            pixels[at+2]=static_cast<std::uint8_t>((0.98f*(1-h)+0.19f*h)*255);
        }
        glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X+face,0,GL_RGB,size,size,0,GL_RGB,GL_UNSIGNED_BYTE,pixels.data());
    }
    glTexParameteri(GL_TEXTURE_CUBE_MAP,GL_TEXTURE_MIN_FILTER,GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_CUBE_MAP,GL_TEXTURE_MAG_FILTER,GL_LINEAR);
    glTexParameteri(GL_TEXTURE_CUBE_MAP,GL_TEXTURE_WRAP_S,GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_CUBE_MAP,GL_TEXTURE_WRAP_T,GL_CLAMP_TO_EDGE);
    glGenerateMipmap(GL_TEXTURE_CUBE_MAP); glBindTexture(GL_TEXTURE_CUBE_MAP,0);
}
void buildPbrCube() {
    cubeVertices.clear();
    const size_t first=static_cast<size_t>((gridCount+6)*6);
    for(size_t i=first;i+17<sceneVertices.size();i+=18) {
        float ax=sceneVertices[i],ay=sceneVertices[i+1],az=sceneVertices[i+2];
        float bx=sceneVertices[i+6],by=sceneVertices[i+7],bz=sceneVertices[i+8];
        float cx=sceneVertices[i+12],cy=sceneVertices[i+13],cz=sceneVertices[i+14];
        float ux=bx-ax,uy=by-ay,uz=bz-az,vx=cx-ax,vy=cy-ay,vz=cz-az;
        float nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;
        float len=std::sqrt(nx*nx+ny*ny+nz*nz); if(len>1e-6f){nx/=len;ny/=len;nz/=len;}
        float centerX=(ax+bx+cx)/3.0f,centerY=(ay+by+cy)/3.0f,centerZ=(az+bz+cz)/3.0f;
        if(nx*centerX+ny*centerY+nz*centerZ<0.0f){nx=-nx;ny=-ny;nz=-nz;}
        for(int v=0;v<3;v++) {
            size_t j=i+static_cast<size_t>(v)*6;
            cubeVertices.insert(cubeVertices.end(),{sceneVertices[j],sceneVertices[j+1],sceneVertices[j+2],nx,ny,nz,sceneVertices[j+3],sceneVertices[j+4],sceneVertices[j+5]});
        }
    }
}

void identity(float* m) {
    for (int i=0;i<16;i++) m[i]=0.0f;
    m[0]=m[5]=m[10]=m[15]=1.0f;
}
void modelMatrix(float* m, const float* transform) {
    if (transform) { for(int i=0;i<16;i++) m[i]=transform[i]; return; }
    identity(m);
}
}
extern "C" __attribute__((visibility("default"))) int shax_init() {
    sceneProgram = linkProgram(sceneVs,sceneFs); pbrProgram=linkProgram(pbrVs,pbrFs); skyProgram=linkProgram(skyVs,skyFs);
    if (!sceneProgram || !pbrProgram || !skyProgram) return 0;
    sceneMvp=glGetUniformLocation(sceneProgram,"uMvp");
    pbrMvp=glGetUniformLocation(pbrProgram,"uMvp"); pbrModel=glGetUniformLocation(pbrProgram,"uModel");
    pbrEnvironment=glGetUniformLocation(pbrProgram,"uEnvironment"); pbrCamera=glGetUniformLocation(pbrProgram,"uCamera"); pbrLight=glGetUniformLocation(pbrProgram,"uLightDirection");
    pbrRoughness=glGetUniformLocation(pbrProgram,"uRoughness"); pbrMetallic=glGetUniformLocation(pbrProgram,"uMetallic");
    skyTime=glGetUniformLocation(skyProgram,"uTime");
    buildGeometry(); buildPbrCube(); buildEnvironment();
    if (!sceneVbo) glGenBuffers(1,&sceneVbo);
    glBindBuffer(GL_ARRAY_BUFFER,sceneVbo);
    glBufferData(GL_ARRAY_BUFFER,static_cast<GLsizeiptr>(sceneVertices.size()*sizeof(float)),
                 sceneVertices.data(),GL_STATIC_DRAW);
    glGenBuffers(1,&cubeVbo); glBindBuffer(GL_ARRAY_BUFFER,cubeVbo);
    glBufferData(GL_ARRAY_BUFFER,static_cast<GLsizeiptr>(cubeVertices.size()*sizeof(float)),cubeVertices.data(),GL_STATIC_DRAW);
    glBindBuffer(GL_ARRAY_BUFFER,0);
    const char* version=reinterpret_cast<const char*>(glGetString(GL_VERSION));
    LOGI("PBR/IBL cube and bounded volumetric sky enabled; driver=%s",version?version:"unknown");
    return glGetError()==GL_NO_ERROR ? 1 : 0;
}
extern "C" __attribute__((visibility("default"))) void shax_set_camera(float x,float y,float z) { cameraX=x; cameraY=y; cameraZ=z; }
extern "C" __attribute__((visibility("default"))) void shax_set_material(float roughness,float metallic) { materialRoughness=std::clamp(roughness,0.04f,1.0f); materialMetallic=std::clamp(metallic,0.0f,1.0f); }
extern "C" __attribute__((visibility("default"))) void shax_resize(int width,int height) {
    glViewport(0,0,width>0?width:1,height>0?height:1);
}
extern "C" __attribute__((visibility("default"))) void shax_draw(const float* viewProjection,
                                                                    const float* cubeModel,
                                                                    float timeSeconds) {
    if (!sceneProgram || !skyProgram || !viewProjection) return;
    const auto cpuStart = std::chrono::steady_clock::now();
    glClearColor(0.03f,0.05f,0.09f,1.0f);
    glClear(GL_COLOR_BUFFER_BIT|GL_DEPTH_BUFFER_BIT);
    glDisable(GL_DEPTH_TEST); glUseProgram(skyProgram);
    if (skyTime>=0) glUniform1f(skyTime,timeSeconds);
    glDrawArrays(GL_TRIANGLES,0,3); glEnable(GL_DEPTH_TEST);
    glUseProgram(sceneProgram);
    glBindBuffer(GL_ARRAY_BUFFER,sceneVbo);
    glEnableVertexAttribArray(0); glEnableVertexAttribArray(1);
    glVertexAttribPointer(0,3,GL_FLOAT,GL_FALSE,6*sizeof(float),nullptr);
    glVertexAttribPointer(1,3,GL_FLOAT,GL_FALSE,6*sizeof(float),reinterpret_cast<void*>(3*sizeof(float)));
    glUniformMatrix4fv(sceneMvp,1,GL_FALSE,viewProjection);
    glDrawArrays(GL_LINES,0,gridCount);
    float model[16]; modelMatrix(model,cubeModel);
    float mvp[16]{};
    for(int col=0;col<4;col++) for(int row=0;row<4;row++)
      for(int k=0;k<4;k++) mvp[col*4+row]+=viewProjection[k*4+row]*model[col*4+k];
    glUseProgram(pbrProgram); glBindBuffer(GL_ARRAY_BUFFER,cubeVbo);
    glEnableVertexAttribArray(0); glEnableVertexAttribArray(1); glEnableVertexAttribArray(2);
    glVertexAttribPointer(0,3,GL_FLOAT,GL_FALSE,9*sizeof(float),nullptr);
    glVertexAttribPointer(1,3,GL_FLOAT,GL_FALSE,9*sizeof(float),reinterpret_cast<void*>(3*sizeof(float)));
    glVertexAttribPointer(2,3,GL_FLOAT,GL_FALSE,9*sizeof(float),reinterpret_cast<void*>(6*sizeof(float)));
    glUniformMatrix4fv(pbrMvp,1,GL_FALSE,mvp); glUniformMatrix4fv(pbrModel,1,GL_FALSE,model);
    if(pbrCamera>=0) glUniform3f(pbrCamera,cameraX,cameraY,cameraZ);
    if(pbrRoughness>=0) glUniform1f(pbrRoughness,materialRoughness);
    if(pbrMetallic>=0) glUniform1f(pbrMetallic,materialMetallic);
    if(pbrLight>=0) glUniform3f(pbrLight,-0.4f,-0.8f,-0.25f);
    glActiveTexture(GL_TEXTURE0); glBindTexture(GL_TEXTURE_CUBE_MAP,environmentCube);
    if(pbrEnvironment>=0) glUniform1i(pbrEnvironment,0);
    glDrawArrays(GL_TRIANGLES,0,static_cast<GLsizei>(cubeVertices.size()/9));
    glDisableVertexAttribArray(2);
    glDisableVertexAttribArray(0); glDisableVertexAttribArray(1);
    glBindBuffer(GL_ARRAY_BUFFER,0);
    static uint64_t frameCounter = 0;
    if ((++frameCounter % 300u) == 0u) {
        const auto cpuUs = std::chrono::duration_cast<std::chrono::microseconds>(std::chrono::steady_clock::now() - cpuStart).count();
        const size_t geometryBytes = (sceneVertices.size() + cubeVertices.size()) * sizeof(float);
        LOGI("CPU render submission=%lld us; geometry memory estimate=%zu bytes; frames=%llu", (long long)cpuUs, geometryBytes, (unsigned long long)frameCounter);
    }
}
extern "C" __attribute__((visibility("default"))) void shax_shutdown() {
    if (sceneVbo) glDeleteBuffers(1,&sceneVbo);
    if (cubeVbo) glDeleteBuffers(1,&cubeVbo);
    if (environmentCube) glDeleteTextures(1,&environmentCube);
    if (pbrProgram) glDeleteProgram(pbrProgram);
    if (sceneProgram) glDeleteProgram(sceneProgram);
    if (skyProgram) glDeleteProgram(skyProgram);
    sceneVbo=cubeVbo=environmentCube=sceneProgram=pbrProgram=skyProgram=0;
}
