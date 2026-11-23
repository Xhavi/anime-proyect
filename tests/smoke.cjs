// Comprueba sintaxis de los scripts inline y la presencia de funciones esenciales.
const fs=require('node:fs');
const vm=require('node:vm');
const assert=require('node:assert/strict');
const html=fs.readFileSync('index.html','utf8');
const matches=[...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/gi)];
assert.ok(matches.length>0,'No se encontraron scripts');
for(const match of matches){new vm.Script(match[1],{filename:'index.html:inline-script'});}
for(const expected of ['function catalog()','function favoritesPage()','function calendar()','function render()','function toggleFavorite(id)']){
  assert.ok(html.includes(expected),'Función faltante: '+expected);
}
assert.ok(html.includes('data-favorite'),'Falta botón de favoritos');
assert.ok(html.includes('aria-label="Buscar anime"'),'Falta nombre accesible del buscador');
assert.ok(!/<script[^>]+src=["']https?:/i.test(html),'Scripts externos inesperados');
console.log('OK: validación estática y sintaxis de JavaScript');
