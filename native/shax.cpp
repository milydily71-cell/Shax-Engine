#include <GLES3/gl3.h>
#include <android/log.h>
#include <array>
#include <cmath>
#include <cstdint>
#include <vector>

#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, "ShaxNative", __VA_ARGS__)
namespace {
GLuint sceneProgram = 0, skyProgram = 0, sceneVbo = 0;
GLint sceneMvp = -1;
GLint skyTime = -1;
GLsizei gridCount = 0, cubeCount = 0;
std::vector<float> sceneVertices;
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
GLuint link(const char* vsSource, const char* fsSource) {
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
constexpr const char* skyVs = R"(#version 300 es
out vec2 vUv;
void main(){ vec2 p=vec2((gl_VertexID<<1)&2,gl_VertexID&2); vUv=p; gl_Position=vec4(p*2.0-1.0,1.0,1.0); })";
constexpr const char* skyFs = R"(#version 300 es
precision highp float;
in vec2 vUv; uniform float uTime; out vec4 fragColor;
void main(){
  float h=clamp(vUv.y,0.0,1.0);
  vec3 horizon=vec3(0.72,0.84,0.98);
  vec3 zenith=vec3(0.025,0.075,0.22);
  vec3 sky=mix(horizon,zenith,pow(h,0.78));
  float glow=exp(-pow((h-0.16)*7.0,2.0))*0.12;
  sky+=vec3(0.16,0.25,0.36)*glow;
  fragColor=vec4(sky,1.0);
})";
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
    sceneProgram = link(sceneVs,sceneFs); skyProgram=link(skyVs,skyFs);
    if (!sceneProgram || !skyProgram) return 0;
    sceneMvp=glGetUniformLocation(sceneProgram,"uMvp");
    skyTime=glGetUniformLocation(skyProgram,"uTime");
    buildGeometry();
    if (!sceneVbo) glGenBuffers(1,&sceneVbo);
    glBindBuffer(GL_ARRAY_BUFFER,sceneVbo);
    glBufferData(GL_ARRAY_BUFFER,static_cast<GLsizeiptr>(sceneVertices.size()*sizeof(float)),
                 sceneVertices.data(),GL_STATIC_DRAW);
    glBindBuffer(GL_ARRAY_BUFFER,0);
    return glGetError()==GL_NO_ERROR ? 1 : 0;
}
extern "C" __attribute__((visibility("default"))) void shax_resize(int width,int height) {
    glViewport(0,0,width>0?width:1,height>0?height:1);
}
extern "C" __attribute__((visibility("default"))) void shax_draw(const float* viewProjection,
                                                                    const float* cubeModel,
                                                                    float timeSeconds) {
    if (!sceneProgram || !skyProgram || !viewProjection) return;
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
    glUniformMatrix4fv(sceneMvp,1,GL_FALSE,mvp);
    glDrawArrays(GL_TRIANGLES,gridCount,cubeCount);
    glDisableVertexAttribArray(0); glDisableVertexAttribArray(1);
    glBindBuffer(GL_ARRAY_BUFFER,0);
}
extern "C" __attribute__((visibility("default"))) void shax_shutdown() {
    if (sceneVbo) glDeleteBuffers(1,&sceneVbo);
    if (sceneProgram) glDeleteProgram(sceneProgram);
    if (skyProgram) glDeleteProgram(skyProgram);
    sceneVbo=sceneProgram=skyProgram=0;
}
