// Native vector sources for the v3 entrance decorations, UI marks and FX.
// npm install sharp@0.35.5, then node Tools/build_throw_v3_vectors.cjs
// DAD_SHARP_MODULE may point at an existing sharp installation.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.env.DAD_SHARP_MODULE || 'sharp');
const root = path.resolve(__dirname, '..');
const art = path.join(root, 'Assets/DadSchoolBoss/Art');
const ink = '#29272b', cream = '#fff2cf', yellow = '#e6ba4d';
const records = {};
const wrap = (w,h,body) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
const polygon = (points,fill=cream,stroke=ink,width=10) => `<polygon points="${points.map(p=>p.map(n=>Math.round(n*100)/100).join(',')).join(' ')}" fill="${fill}" stroke="${stroke}" stroke-width="${width}" stroke-linejoin="round"/>`;
const line = (x1,y1,x2,y2,color=ink,width=10) => `<path d="M${x1} ${y1}L${x2} ${y2}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round"/>`;
function flowerCoin(cx,cy,r,opacity=1) {
  let petals='';
  for(let i=0;i<5;i++){let a=-Math.PI/2+i*Math.PI*2/5;petals+=`<circle cx="${cx+Math.cos(a)*r*.23}" cy="${cy+Math.sin(a)*r*.23}" r="${r*.19}" fill="${cream}" stroke="${ink}" stroke-width="${r*.045}"/>`;}
  return `<g opacity="${opacity}"><circle cx="${cx}" cy="${cy}" r="${r}" fill="${yellow}" stroke="${ink}" stroke-width="${r*.12}"/><path d="M${cx-r*.82} ${cy+r*.35} Q${cx} ${cy+r*1.28} ${cx+r*.88} ${cy+r*.1} Q${cx+r*.6} ${cy+r*.95} ${cx} ${cy+r*.93} Q${cx-r*.6} ${cy+r*.95} ${cx-r*.82} ${cy+r*.35}" fill="#b78434"/><circle cx="${cx}" cy="${cy}" r="${r*.72}" fill="none" stroke="${ink}" stroke-width="${r*.055}"/>${petals}<circle cx="${cx}" cy="${cy}" r="${r*.12}" fill="${yellow}" stroke="${ink}" stroke-width="${r*.04}"/></g>`;
}
async function save(folder,id,w,h,body,metadata={}) {
  const dir=path.join(art,folder);fs.mkdirSync(path.join(dir,'sources'),{recursive:true});
  const svg=wrap(w,h,body);fs.writeFileSync(path.join(dir,'sources',id+'.svg'),svg);
  await sharp(Buffer.from(svg)).png().toFile(path.join(dir,id+'.png'));
  (records[folder] ||= []).push({id,file:id+'.png',width:w,height:h,sourceType:'vector',source:'sources/'+id+'.svg',pivotPixels:{x:w/2,y:h/2},...metadata});
}
async function main(){
  let band='';
  // Full-width strokes have identical first/last columns: seamless horizontal repeat.
  for(let i=0;i<13;i++){const y=22+i*38; const t=[8,4,14,5,9][i%5];band+=`<rect x="0" y="${y}" width="2048" height="${t}" fill="${i%2?'#c4952d':'#fff9df'}"/>`;}
  for(let i=0;i<12;i++){const y=37+i*39,x=130+(i*263)%1450;band+=polygon([[x,y],[x+270,y-5],[x+400,y],[x+270,y+5]],i%3?'#fff9df':'#c4952d','none',0);}
  await save('ThrowEntranceV1','entrance_band_lines',2048,512,band,{tileX:true});
  let burst='';
  for(let i=0;i<36;i++){const a=i*Math.PI*2/36; const r0=355+(i%3)*23,r1=742-(i%4)*15,half=.022+(i%3)*.004;burst+=polygon([[768+Math.cos(a)*r0,768+Math.sin(a)*r0],[768+Math.cos(a-half)*r1,768+Math.sin(a-half)*r1],[768+Math.cos(a+half)*r1,768+Math.sin(a+half)*r1]],cream,ink,7);}
  await save('ThrowEntranceV1','entrance_burst',1536,1536,burst,{emptyCenterRadius:340});
  // 96px fixed corners, straight stretch bands and lower-right tail.
  const balloon=[[16,85],[47,63],[32,29],[76,43],[92,16],[118,40],[906,40],[929,18],[946,45],[987,30],[974,65],[1007,84],[987,108],[992,214],[1008,239],[976,244],[984,300],[935,260],[915,278],[895,257],[117,257],[93,283],[78,256],[36,272],[46,238],[16,226],[34,199],[33,112]];
  await save('ThrowEntranceV1','entrance_balloon',1024,320,polygon(balloon,cream,ink,11),{nineSlice:{left:96,top:96,right:96,bottom:96},textSafeRect:[110,96,914,224]});
  await save('ThrowShopV1','icon_won',256,256,flowerCoin(128,128,98));
  let shop=`<path d="M47 114H209V222H47Z" fill="${cream}" stroke="${ink}" stroke-width="12"/><path d="M75 151H127V222H75Z" fill="#5b6c7b" stroke="${ink}" stroke-width="9"/><path d="M150 150H185V183H150Z" fill="#a4b4b5" stroke="${ink}" stroke-width="8"/>`;
  for(let i=0;i<6;i++){const x=28+i*33.333;shop+=`<path d="M${x+15} 44H${x+33.333-3}L${x+33.333} 105Q${x+16.67} 130 ${x} 105Z" fill="${i%2?cream:'#b85c48'}" stroke="${ink}" stroke-width="6"/>`;}
  shop+=line(44,44,212,44,ink,12)+line(29,224,227,224,ink,12);
  await save('ThrowShopV1','icon_shop',256,256,shop);
  await save('ThrowShopV1','mark_soldout',256,256,`<circle cx="128" cy="128" r="99" fill="${cream}" stroke="${ink}" stroke-width="13"/><circle cx="128" cy="128" r="79" fill="none" stroke="#81795d" stroke-width="5"/><path d="M78 129L112 164L181 92" fill="none" stroke="${ink}" stroke-width="23" stroke-linejoin="round" stroke-linecap="round"/>`);
  for(let f=0;f<3;f++){
    const c=128,scale=[.66,.9,1][f],alpha=[1,.85,.42][f];let shape='';
    const p=[];for(let i=0;i<12;i++){let a=-Math.PI/2+i*Math.PI/6,r=(i%2?18:57)*scale;p.push([c+Math.cos(a)*r,c+Math.sin(a)*r]);}
    shape+=polygon(p,'#fff9e9',ink,7);
    for(let j=0;j<5;j++){const a=-2.5+j*1.19,near=(66+f*8),far=(89+f*10);shape+=line(c+Math.cos(a)*near,c+Math.sin(a)*near,c+Math.cos(a)*far,c+Math.sin(a)*far,j%2?cream:'#ebc35d',7-f);}
    await save('ThrowFxV2',`fx_tray_clang_${f}`,256,256,`<g opacity="${alpha}">${shape}</g>`,{animation:'fx_tray_clang',frame:f,durationMs:100,pivotPixels:{x:128,y:128}});
    const cy=[82,55,30][f],r=[25,23,17][f];let coin=flowerCoin(64,cy,r,[1,.9,.45][f]);
    if(f>0){coin+=line(27,cy-5,27,cy+9,cream,4)+line(20,cy+2,34,cy+2,cream,4)+line(99,cy+17,99,cy+29,yellow,4)+line(93,cy+23,105,cy+23,yellow,4);}
    await save('ThrowFxV2',`fx_coin_pop_${f}`,128,128,coin,{animation:'fx_coin_pop',frame:f,durationMs:100,pivotPixels:{x:64,y:108}});
  }
  fs.mkdirSync(path.join(root,'Build/ThrowAssetsV3'),{recursive:true});
  fs.writeFileSync(path.join(root,'Build/ThrowAssetsV3/vector-records.json'),JSON.stringify(records,null,2)+'\n');
  console.log('Rendered 12 native SVG assets to exact PNG dimensions.');
}
main().catch(e=>{console.error(e);process.exitCode=1});
