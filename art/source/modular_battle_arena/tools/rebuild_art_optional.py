from __future__ import annotations
import math, json, shutil, hashlib
from pathlib import Path
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFilter, ImageOps, ImageFont

ROOT=Path(__file__).resolve().parents[1]
for name in ['assets/ground','assets/backdrops','assets/edges','assets/structures','assets/fx','assets/assembled','sources','metadata','tools','docs','previews']:
 (ROOT/name).mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'sources/arena_painting_original.png'
REF=ROOT/'sources/approved_battle_reference.png'
# Input already stored in sources/.
# Input already stored in sources/.
records=[]

def save(im,path,**extra):
 path=ROOT/path
 im.save(path,optimize=True)
 a=im.getchannel('A') if im.mode=='RGBA' else None
 records.append(dict(path=path.relative_to(ROOT).as_posix(),size=list(im.size),mode=im.mode,alpha_extrema=list(a.getextrema()) if a else None,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),**extra))

def smooth(t):
 t=np.clip(t,0,1);return t*t*(3-2*t)

def periodic(arr,axis=1,width=112):
 b=np.moveaxis(arr.astype(np.float32),axis,1).copy()
 n=b.shape[1];width=min(width,n//3)
 l=b[:,:width].copy();rr=b[:,-width:][:,::-1].copy()
 t=.5*(1-smooth(np.linspace(0,1,width)))
 t=t[None,:,None]
 b[:,:width]=l*(1-t)+rr*t
 b[:,-width:]=(rr*(1-t)+l*t)[:,::-1]
 return np.moveaxis(np.rint(b).clip(0,255).astype(np.uint8),1,axis)

def common_edges(arr,master,xwidth=196,ywidth=0):
 h,w=arr.shape[:2]
 xx=np.minimum(np.arange(w),np.arange(w)[::-1])
 wx=1-smooth((xx-xwidth*.35)/(xwidth*.65))
 weight=np.broadcast_to(wx[None,:],(h,w)).copy()
 if ywidth:
  yy=np.minimum(np.arange(h),np.arange(h)[::-1])
  wy=1-smooth((yy-ywidth*.35)/(ywidth*.65))
  weight=np.maximum(weight,wy[:,None])
 weight=weight[:,:,None]
 return np.rint(arr*(1-weight)+master*weight).clip(0,255).astype('uint8')

source=Image.open(SOURCE).convert('RGB')
base=np.array(source.crop((0,390,1024,1180)).resize((1024,1024),Image.Resampling.LANCZOS))
base=periodic(periodic(base,1,128),0,128)
grounds=[]
for i,(dx,dy) in enumerate([(0,0),(271,173),(609,389)],1):
 a=base.copy() if i==1 else common_edges(np.roll(base,(dy,dx),(0,1)),base,196,196)
 im=Image.fromarray(a,'RGB');grounds.append(im)
 save(im,f'assets/ground/ground_repeat_{i:02d}.png',repeat_axes=['x','y'],family='ground',variant_method='Source phase variation; common edge collars')

back=np.array(source.crop((0,0,1024,400)).resize((1024,384),Image.Resampling.LANCZOS))
back=periodic(back,1,128)
backs=[]
for i,dx in enumerate([0,317,683],1):
 a=back.copy() if i==1 else common_edges(np.roll(back,dx,1),back,196)
 alpha=np.round((1-smooth((np.arange(384)-238)/145))*255).astype('uint8')
 rgba=np.dstack([a,np.broadcast_to(alpha[:,None],(384,1024))])
 rgba[rgba[:,:,3]==0,:3]=0
 im=Image.fromarray(rgba,'RGBA');backs.append(im)
 save(im,f'assets/backdrops/backdrop_repeat_{i:02d}.png',repeat_axes=['x'],family='backdrop',bottom_fade_px=145)

front=np.array(source.crop((0,1216,1024,1536)))
front=periodic(front,1,128)
fa=np.round(smooth((np.arange(320)-12)/112)*255).astype('uint8')
rgba=np.dstack([front,np.broadcast_to(fa[:,None],(320,1024))]);rgba[rgba[:,:,3]==0,:3]=0
fg=Image.fromarray(rgba,'RGBA')
save(fg,'assets/edges/foreground_repeat.png',repeat_axes=['x'],family='foreground',top_fade_px=124)

# Endcaps are decorative edge strips from the approved concept, not playable sectors.
ref=Image.open(REF).convert('RGB')
cap=ref.crop((0,0,258,941)).resize((352,1280),Image.Resampling.LANCZOS)
a=np.array(cap)
# Fades towards playfield and at top/bottom; never cover playable cells by default.
cx=(1-smooth((np.arange(352)-220)/131))
cy=np.ones(1280);cy[:50]=smooth(np.arange(50)/49);cy[-50:]=smooth(np.arange(50)[::-1]/49)
ca=np.round(cx[None,:]*cy[:,None]*255).astype('uint8')
rgba=np.dstack([a,ca]);rgba[ca==0,:3]=0
left=Image.fromarray(rgba,'RGBA');right=ImageOps.mirror(left)
save(left,'assets/edges/arena_left_cap.png',repeat_axes=[],family='endcap',placement='outside global left board bound')
save(right,'assets/edges/arena_right_cap.png',repeat_axes=[],family='endcap',placement='outside global right board bound',derived_from='mirrored left edge; decorative only')

# Cut two standalone structures from the user-approved reference.
# Color matting only outside explicit protected structure masks.
def cut_structure(crop,outline,protected_polys,ground,name):
 im=ref.crop(crop);a=np.array(im);h,w=a.shape[:2]
 hsv=cv2.cvtColor(a,cv2.COLOR_RGB2HSV)
 mask=np.full((h,w),cv2.GC_BGD,dtype=np.uint8)
 cv2.fillPoly(mask,[np.array(outline,np.int32)],cv2.GC_PR_FGD)
 neutral=(hsv[:,:,1]<36)&(hsv[:,:,2]<100)
 mask[(mask==cv2.GC_PR_FGD)&neutral]=cv2.GC_PR_BGD
 mask[(hsv[:,:,1]>85)&(hsv[:,:,2]>55)&(mask!=0)]=cv2.GC_FGD
 protected=np.zeros((h,w),np.uint8)
 for poly in protected_polys:
  cv2.fillPoly(mask,[np.array(poly,np.int32)],cv2.GC_FGD)
  cv2.fillPoly(protected,[np.array(poly,np.int32)],255)
 cv2.setRNGSeed(31)
 cv2.grabCut(a,mask,None,np.zeros((1,65)),np.zeros((1,65)),5,cv2.GC_INIT_WITH_MASK)
 f=np.where((mask==1)|(mask==3),255,0).astype('uint8')
 _,labels,stats,_=cv2.connectedComponentsWithStats(f,8)
 for j in range(1,len(stats)):
  if stats[j,cv2.CC_STAT_AREA]<3:f[labels==j]=0
 alpha=np.array(Image.fromarray(f).filter(ImageFilter.GaussianBlur(.35)))/255
 y,x=np.mgrid[0:h,0:w].astype(float);x/=w;y/=h
 feat=np.stack([np.ones_like(x),x,y,x*x,x*y,y*y],-1)
 sample=neutral&(hsv[:,:,2]>26)&(hsv[:,:,2]<65)&(protected==0)
 coef=np.linalg.lstsq(feat[sample],a[sample].astype(float),rcond=None)[0]
 bg=feat@coef
 diff=np.max(np.abs(a.astype(float)-bg),axis=-1)
 aa=np.clip((diff-4)/20,0,1);aa[protected>0]=1
 aa=np.minimum(aa,alpha)
 # Diffuse glows are partial-alpha light, not opaque blocks of the source backdrop.
 halo_polys = ([[(185,52),(259,49),(267,151),(198,150)],[(447,305),(491,328),(488,397),(446,388)]] if name=='keep' else [[(300,121),(345,128),(367,167),(409,147),(447,141),(421,195),(306,180)]])
 hm=np.zeros((h,w),np.uint8)
 for poly in halo_polys: cv2.fillPoly(hm,[np.array(poly,np.int32)],255)
 hm=np.array(Image.fromarray(hm).filter(ImageFilter.GaussianBlur(4)))/255
 hm[protected>0]=0
 soft=np.minimum(aa,np.clip(diff/112,0,1)**1.15)
 aa=aa*(1-hm)+soft*hm
 rgb=np.clip(bg+(a.astype(float)-bg)/np.maximum(aa[:,:,None],.1),0,255).astype('uint8');rgb[aa<.01]=0
 cut=Image.fromarray(np.dstack([rgb,np.round(aa*255).astype('uint8')]),'RGBA')
 cut.save(ROOT/f'sources/{name}_cutout.png',optimize=True)
 # Keep actual registration point throughout normalization, not the lowest shadow.
 gx,gy=ground;ax,ay=256,430
 scale=min((ax-16)/max(gx,1),(512-ax-16)/max(w-gx,1),(ay-16)/max(gy,1),(512-ay-16)/max(h-gy,1))
 size=(round(w*scale),round(h*scale));offset=(round(ax-gx*scale),round(ay-gy*scale))
 canvas=Image.new('RGBA',(512,512));canvas.alpha_composite(cut.resize(size,Image.Resampling.LANCZOS),offset)
 save(canvas,f'assets/structures/{name}.png',anchor=[.5,430/512],source_ground_px=list(ground),source_crop=list(crop),normalize_scale=scale,normalize_offset=list(offset),role='static_structure_sprite')
 return canvas

keep=cut_structure((1170,0,1672,500),
 [[58,25],[212,18],[245,34],[290,15],[395,16],[449,93],[470,174],[464,288],[492,440],[484,477],[379,493],[119,492],[23,470],[19,380],[47,260],[54,175],[48,85]],
 [[(196,162),(282,176),(324,319),(355,423),(150,425),(157,324)],[(75,424),(331,403),(440,451),(386,477),(109,469)]],
 (257,438),'keep')
tower=cut_structure((1170,540,1672,864),
 [[224,3],[293,17],[329,49],[316,90],[339,121],[440,139],[468,269],[435,297],[336,321],[150,319],[38,289],[56,238],[73,159],[140,172],[155,88],[210,48]],
 [[(185,110),(287,111),(301,238),(283,271),(167,265),(170,179)],[(93,248),(220,234),(351,244),(412,274),(354,302),(157,301),(75,282)]],
 (250,278),'defense_tower')

# Readability masks and ambience are technical raster textures, deterministic.
rng=np.random.default_rng(20261020)
S=512
# Rain: periodic wrapping in both directions.
rain=Image.new('RGBA',(S,S));draw=ImageDraw.Draw(rain)
for _ in range(85):
 x,y=rng.uniform(0,S,2);length=rng.uniform(9,30);dx=-length*.28
 alpha=int(rng.integers(18,74))
 for ox in [-S,0,S]:
  for oy in [-S,0,S]:draw.line((x+ox,y+oy,x+dx+ox,y+length+oy),fill=(169,196,215,alpha),width=1)
save(rain,'assets/fx/rain_repeat.png',repeat_axes=['x','y'],family='weather',suggested_opacity=.3)
embers=Image.new('RGBA',(S,S));draw=ImageDraw.Draw(embers)
for _ in range(26):
 x,y=rng.uniform(0,S,2);rr=float(rng.uniform(.8,2.2));alpha=int(rng.integers(40,175))
 for ox in [-S,0,S]:
  for oy in [-S,0,S]:draw.ellipse((x-rr+ox,y-rr+oy,x+rr+ox,y+rr+oy),fill=(255,117,42,alpha))
embers=Image.alpha_composite(embers.filter(ImageFilter.GaussianBlur(2.4)),embers)
save(embers,'assets/fx/embers_repeat.png',repeat_axes=['x','y'],family='weather',suggested_opacity=.45)
noise=np.zeros((S,S),float)
for sig,weight in [(15,.2),(35,.35),(75,.45)]:
 from scipy.ndimage import gaussian_filter
 z=gaussian_filter(rng.normal(size=(S,S)),sig,mode='wrap');z=(z-z.min())/(z.max()-z.min());noise+=z*weight
smokea=np.round(smooth((noise-.25)/.65)*60).clip(0,60).astype('uint8')
smoke=Image.new('RGBA',(S,S),(102,108,119,0));smoke.putalpha(Image.fromarray(smokea))
save(smoke,'assets/fx/smoke_repeat.png',repeat_axes=['x','y'],family='weather',suggested_opacity=.24)
for name,col in [('fire_glow',(255,112,43)),('crystal_glow',(64,222,255))]:
 y,x=np.mgrid[-1:1:complex(256),-1:1:complex(256)]
 alpha=np.round(np.exp(-6*(x*x+y*y))*smooth((1-np.sqrt(x*x+y*y))/.25)*150).clip(0,255).astype('uint8')
 glow=Image.new('RGBA',(256,256),col+(0,));glow.putalpha(Image.fromarray(alpha));save(glow,f'assets/fx/{name}.png',repeat_axes=[],family='glow')

# Quick-start plates are alternatives to (not additions to) the layered strips.
for i in range(3):
 plate=Image.new('RGBA',(1024,1536));g=grounds[i].convert('RGBA')
 plate.alpha_composite(g,(0,0));plate.alpha_composite(g,(0,1024));plate.alpha_composite(backs[i],(0,0));plate.alpha_composite(fg,(0,1216))
 save(plate.convert('RGB'),f'assets/assembled/arena_mid_{i+1:02d}.png',repeat_axes=['x'],family='assembled_plate',alternative_to_layered=True)

(ROOT/'metadata/assets.json').write_text(json.dumps({'schema':'common-watch-arena-assets/1','assets':records},indent=2)+'\n')
print('Saved',len(records),'runtime PNGs to',ROOT)
