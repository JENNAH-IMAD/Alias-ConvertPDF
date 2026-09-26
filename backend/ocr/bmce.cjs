const fs=require('fs');const path=require('path');const sharp=require('sharp');const {createWorker,PSM}=require('tesseract.js');
function groups(values){const out=[];for(const v of values){if(!out.length||v>out.at(-1).at(-1)+1)out.push([v]);else out.at(-1).push(v);}return out.map(g=>Math.round((g[0]+g.at(-1))/2));}
async function run(files){const worker=await createWorker('eng',1,{langPath:path.join(__dirname,'tessdata'),gzip:false,cacheMethod:'none'});const results=[];try{await worker.setParameters({tessedit_pageseg_mode:PSM.SINGLE_BLOCK,user_defined_dpi:'300'});
 for(const file of files){const {data,info}=await sharp(file).greyscale().raw().toBuffer({resolveWithObject:true});const {width:w,height:h}=info;
 const ys=[];for(let y=0;y<h;y++){let count=0;for(let x=0;x<w;x++)if(data[y*w+x]<210)count++;if(count>w*.5)ys.push(y);}const bands=[];for(const y of ys){if(!bands.length||y>bands.at(-1).at(-1)+1)bands.push([y]);else bands.at(-1).push(y);}let lines=bands.flatMap(g=>g.length>5?[g[0],g.at(-1)]:[Math.round((g[0]+g.at(-1))/2)]);let start=0;for(let i=1;i<lines.length;i++)if(lines[i]-lines[i-1]>70)start=i;lines=lines.slice(start);if(lines.length<3){results.push({file,rows:[],error:'Grille non détectée'});continue;}
 const top=lines[0],bottom=lines.at(-1);const xs=[];for(let x=0;x<w;x++){let count=0;for(let y=top;y<=bottom;y++)if(data[y*w+x]<210)count++;if(count/(bottom-top+1)>.73)xs.push(x);}const cols=groups(xs);if(cols.length!==6){results.push({file,rows:[],error:'Colonnes: '+cols.join(','),lines});continue;}
 const rows=[];for(let r=0;r<lines.length-1;r++){const y=lines[r]+2,height=lines[r+1]-y-1;if(height<9)continue;const cells=[];for(let c=0;c<5;c++){const left=cols[c]+2,width=cols[c+1]-left-1;const image=await sharp(file).extract({left,top:y,width,height}).resize({width:width*3}).flatten({background:'#ffffff'}).normalise().png().toBuffer();const result=await worker.recognize(image);cells.push(result.data.text.trim());}rows.push(cells);}results.push({file,rows,lines,cols});
 }return results;}finally{await worker.terminate();}}
run(process.argv.slice(2)).then(r=>process.stdout.write(JSON.stringify(r))).catch(e=>{console.error(e.message);process.exit(1);});


