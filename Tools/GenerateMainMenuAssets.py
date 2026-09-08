"""Deterministic OWNED stencils and replaceable can Foley. Run from project root."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import random, math, wave, struct

ROOT = Path('Assets/09_MainMenu')
(ROOT/'Textures').mkdir(parents=True, exist_ok=True)
(ROOT/'Audio').mkdir(parents=True, exist_ok=True)
font_path = 'Assets/05_Tipografias/owned.ttf'
for index,word in enumerate(['JUGAR','GALERÍA','CONFIG','SALIR']):
    font=ImageFont.truetype(font_path,260)
    bounds=font.getbbox(word)
    glyph=Image.new('L',(bounds[2]-bounds[0]+20,bounds[3]-bounds[1]+20))
    ImageDraw.Draw(glyph).text((10-bounds[0],10-bounds[1]),word,font=font,fill=255)
    glyph.thumbnail((920,245),Image.Resampling.LANCZOS)
    mask=Image.new('L',(1024,384))
    x=(1024-glyph.width)//2; y=(320-glyph.height)//2
    mask.paste(glyph,(x,y))
    halo=mask.filter(ImageFilter.MaxFilter(19)).filter(ImageFilter.GaussianBlur(7))
    drips=Image.new('L',mask.size); draw=ImageDraw.Draw(drips); rng=random.Random(981+index)
    for _ in range(16):
        column=rng.randrange(x+10,x+glyph.width-10)
        ys=[row for row in range(70,310) if mask.getpixel((column,row))>160]
        if not ys: continue
        start=max(ys); end=min(378,start+rng.randrange(12,67)); width=rng.randrange(1,4)
        draw.line((column,start,column+.5,end),fill=rng.randrange(140,240),width=width)
        draw.ellipse((column-width,end-width,column+width,end+width),fill=190)
    # R = actual OWNED glyph, G = soft overspray, B = ink drips.
    Image.merge('RGB',(mask,halo,drips)).save(ROOT/'Textures'/f'{index:02d}_{word}.png')

rate=44100
for name,duration in [('Can_Rattle',.36),('Can_Clack',.12),('Can_Spray',.68)]:
    rng=random.Random(192); samples=[]; filtered=0
    for i in range(int(rate*duration)):
        t=i/rate; noise=rng.uniform(-1,1); filtered=.91*filtered+.09*noise
        if name=='Can_Rattle':
            value=0
            for hit in [.008,.046,.095,.163,.247]:
                age=t-hit
                if age>=0:value+=(math.sin(2*math.pi*2430*age)+.4*math.sin(2*math.pi*3910*age)+noise*.5)*math.exp(-age*125)*.23
        elif name=='Can_Clack':
            value=(math.sin(2*math.pi*1340*t)+.35*math.sin(2*math.pi*3770*t)+noise*.6)*math.exp(-t*90)*.3
        else:
            envelope=min(1,t/.018)*min(1,(duration-t)/.10)
            value=(noise-filtered)*(.85+math.sin(t*41)*.08)*envelope*.23
        samples.append(struct.pack('<h',int(max(-.95,min(.95,value))*32767)))
    with wave.open(str(ROOT/'Audio'/f'{name}.wav'),'wb') as output:
        output.setparams((1,2,rate,0,'NONE','not compressed'));output.writeframes(b''.join(samples))
print('4 OWNED stencil masks and 3 original synthetic Foley clips generated.')
