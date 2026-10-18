/* трябва да налеем тук книги от базата данни като я направим */
const items = [
    { t: 'Тихите стаи', a: 'Мария Георгиева', c: '#F1A8B2', f: '#472950', pdf: '#' },
    { t: 'Лилав час', a: 'Иван Петров', c: '#DDC3E0', f: '#472950', pdf: '#' },
    { t: 'Сол и канела', a: 'Елена Димова', c: '#78201B', f: '#FFEBE1', pdf: '#' },
    { t: 'Нощен влак', a: 'Борис Стоянов', c: '#472950', f: '#FFEBE1', pdf: '#' },
    { t: 'Кайсиева градина', a: 'Ана Колева', c: '#FFEBE1', f: '#78201B', pdf: '#' },
    { t: 'Писма до лятото', a: 'Георги Николов', c: '#A5527A', f: '#FFEBE1', pdf: '#' },
    { t: 'Малки бури', a: 'Десислава Иванова', c: '#8E5A9A', f: '#472950', pdf: '#' },
    { t: 'Дървета и дим', a: 'Петър Андреев', c: '#2E1A36', f: '#DDC3E0', pdf: '#' },
    { t: 'Празна лавица', a: 'Силвия Томова', c: '#8E3B46', f: '#FFEBE1', pdf: '#' }
];

const wrap = document.getElementById('books'), shelf = document.getElementById('shelf');
const capT = document.getElementById('capTitle'), capA = document.getElementById('capAuthor'), capO = document.getElementById('capOpen');
const btnPrev = document.getElementById('prev'), btnNext = document.getElementById('next');
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
const last = items.length - 1;
const GAP = 6;
let SPINE_W = 34, COVER_W = 200, target = 3, pos = 3, running = false, lastShown = -1;

const books = items.map(it => {
    const b = document.createElement('div');
    b.className = 'book';
    b.innerHTML = `<div class="spine" style="background:${it.c};color:${it.f}">${it.t}</div>
    <div class="cover" style="background:${it.c};color:${it.f}"><div class="ct">${it.t}</div><div class="ca">${it.a}</div></div>`;
    wrap.appendChild(b);
    return b;
});

function size() {
    COVER_W = clamp(wrap.clientWidth * 0.36, 140, 220);
    books.forEach(b => b.style.setProperty('--cover-w', COVER_W + 'px'));
}

function draw() {
    const ts = books.map((_, i) => clamp(1 - Math.abs(i - pos), 0, 1));
    const widths = ts.map(t => SPINE_W + (COVER_W - SPINE_W) * t);
    let x = 0;
    const lefts = widths.map(w => { const l = x; x += w + GAP; return l; });
    const i0 = Math.floor(pos), i1 = Math.min(i0 + 1, last), f = pos - i0;
    const c0 = lefts[i0] + widths[i0] / 2, c1 = lefts[i1] + widths[i1] / 2;
    const offset = wrap.clientWidth / 2 - (c0 + (c1 - c0) * f);

    books.forEach((b, i) => {
        const t = ts[i], h = 78 + 22 * t + (i % 3) * 3 * (1 - t);   // лека разлика във височините
        b.style.width = widths[i] + 'px';
        b.style.height = h + '%';
        b.style.transform = `translateX(${lefts[i] + offset}px)`;
        b.style.zIndex = Math.round(t * 10);
        b.children[1].style.transform = `rotateY(${-90 * (1 - t)}deg)`;   // корица
        b.children[0].style.opacity = 1 - t;                              // гръб
    });

    const cur = Math.round(pos);
    if (cur !== lastShown) {
        lastShown = cur;
        if (capT) capT.textContent = items[cur].t;
        if (capA) capA.textContent = items[cur].a;
        if (capO) capO.href = items[cur].pdf;   
    }
}

function loop() {
    pos += (target - pos) * 0.12;
    if (Math.abs(target - pos) < 0.001) { pos = target; running = false; }
    try {
        draw();
    } finally {
        
        if (running) requestAnimationFrame(loop);
    }
}
function kick() { if (!running) { running = true; requestAnimationFrame(loop); } }
function set(v) { target = clamp(v, 0, last); kick(); }

if (btnNext) btnNext.onclick = () => set(Math.round(target) + 1);
if (btnPrev) btnPrev.onclick = () => set(Math.round(target) - 1);

shelf.addEventListener('wheel', e => {
    if (Math.abs(e.deltaX) > Math.abs(e.deltaY) || e.shiftKey) { e.preventDefault(); set(target + (e.deltaX || e.deltaY) / 300); }
}, { passive: false });
shelf.addEventListener('keydown', e => { if (e.key === 'ArrowRight') set(Math.round(target) + 1); if (e.key === 'ArrowLeft') set(Math.round(target) - 1); });
shelf.tabIndex = 0;

let startX = null, startT = 0, moved = false;
shelf.addEventListener('pointerdown', e => { startX = e.clientX; startT = target; moved = false; });
window.addEventListener('pointermove', e => {
    if (startX === null) return;
    if (Math.abs(e.clientX - startX) > 5) { moved = true; shelf.classList.add('dragging'); }
    if (moved) set(startT - (e.clientX - startX) / 110);
});
window.addEventListener('pointerup', () => {
    if (startX === null) return;
    startX = null; shelf.classList.remove('dragging');
    if (moved) set(Math.round(target));
});
shelf.addEventListener('click', e => {
    if (moved) return;
    const i = books.indexOf(e.target.closest('.book'));
    if (i >= 0) set(i);
});
window.addEventListener('resize', () => { size(); draw(); });

size(); draw();