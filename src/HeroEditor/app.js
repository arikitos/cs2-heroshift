const PATHS = {
  skills: ['src', 'HeroEditor', 'skills.generated.json'],
  localization: ['src', 'HeroShift', 'Localization', 'Resources', 'en.json'],
  config: ['config', 'heroshift.json'],
  bindings: ['src', 'HeroEditor', 'description.bindings.json'],
};

const state = {
  mode: null,
  root: null,
  handles: {},
  skills: [],
  localization: {},
  config: {},
  overrides: {},
  bindings: {},
  defaultBindings: {},
  drafts: {},
  nextVersion: null,
  sessionToken: null,
  dirty: false,
  busy: false,
};

const $ = selector => document.querySelector(selector);
const ui = {
  status: $('#status'), search: $('#search'), reset: $('#resetAllBtn'), save: $('#saveBtn'),
  publish: $('#publishBtn'), open: $('#openBtn'), subbar: $('#subbar'), rarity: $('#rarityFilter'),
  overridden: $('#overriddenOnly'), version: $('#versionLabel'), count: $('#count'),
  welcome: $('#welcome'), grid: $('#grid'), modal: $('#resultModal'), modalTitle: $('#resultTitle'),
  modalOutput: $('#resultOutput'),
};

const clone = value => JSON.parse(JSON.stringify(value));

function status(text, kind = '') {
  ui.status.textContent = text;
  ui.status.className = kind;
}

function markDirty() {
  state.dirty = true;
  status('Unsaved changes', 'dirty');
  updateControls();
}

function updateControls() {
  const loaded = state.skills.length > 0;
  ui.search.disabled = !loaded;
  ui.reset.disabled = !loaded || state.busy;
  ui.save.disabled = !loaded || !state.dirty || state.busy;
  ui.publish.disabled = !loaded || state.mode !== 'api' || state.busy;
  ui.subbar.classList.toggle('visible', loaded);
  ui.version.textContent = state.nextVersion ? `Next local version: ${state.nextVersion}` : '';
}

function showResult(title, output) {
  ui.modalTitle.textContent = title;
  ui.modalOutput.textContent = output;
  ui.modal.classList.add('visible');
}

function openDatabase() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open('HeroShiftEditor', 1);
    request.onupgradeneeded = () => request.result.createObjectStore('handles');
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

async function storeHandle(handle) {
  const database = await openDatabase();
  await new Promise((resolve, reject) => {
    const transaction = database.transaction('handles', 'readwrite');
    transaction.objectStore('handles').put(handle, 'projectRoot');
    transaction.oncomplete = resolve;
    transaction.onerror = () => reject(transaction.error);
  });
  database.close();
}

async function storedHandle() {
  const database = await openDatabase();
  const handle = await new Promise((resolve, reject) => {
    const request = database.transaction('handles', 'readonly').objectStore('handles').get('projectRoot');
    request.onsuccess = () => resolve(request.result || null);
    request.onerror = () => reject(request.error);
  });
  database.close();
  return handle;
}

async function fileHandle(root, parts) {
  let directory = root;
  for (const part of parts.slice(0, -1)) directory = await directory.getDirectoryHandle(part);
  return directory.getFileHandle(parts.at(-1), { create: false });
}

async function readJson(handle) {
  return JSON.parse(await (await handle.getFile()).text());
}

async function writeJson(handle, value) {
  const writable = await handle.createWritable();
  await writable.write(JSON.stringify(value, null, 2) + '\n');
  await writable.close();
}

async function loadFromFolder(root) {
  const handles = {};
  for (const [name, path] of Object.entries(PATHS)) handles[name] = await fileHandle(root, path);
  state.mode = 'file';
  state.root = root;
  state.handles = handles;
  initialize({
    skills: await readJson(handles.skills),
    localization: await readJson(handles.localization),
    config: await readJson(handles.config),
    bindings: await readJson(handles.bindings),
    nextVersion: null,
    sessionToken: null,
  });
}

async function loadFromApi() {
  const response = await fetch('/api/project', { cache: 'no-store' });
  if (!response.ok) throw new Error(await response.text());
  state.mode = 'api';
  initialize(await response.json());
}

function initialize(payload) {
  state.skills = payload.skills || [];
  state.localization = payload.localization || {};
  state.config = payload.config || { schemaVersion: 1 };
  state.overrides = state.config.skills || {};
  state.config.skills = state.overrides;
  state.bindings = payload.bindings || {};
  state.defaultBindings = clone(state.bindings);
  state.nextVersion = payload.nextVersion || null;
  state.sessionToken = payload.sessionToken || null;
  state.drafts = {};
  for (const skill of state.skills) {
    state.drafts[skill.id] = state.bindings[skill.id] || state.localization[`${skill.id}_desc`] || skill.description || '';
  }
  state.dirty = false;
  ui.open.style.display = 'none';
  ui.welcome.style.display = 'none';
  status(`Loaded ${state.skills.length} skills`, 'saved');
  updateControls();
  render();
}

async function chooseFolder() {
  if (!window.showDirectoryPicker) {
    alert('Use Chrome or Edge, or start the editor through start.ps1.');
    return;
  }
  try {
    const root = await window.showDirectoryPicker({ mode: 'readwrite' });
    await storeHandle(root);
    await loadFromFolder(root);
  } catch (error) {
    if (error.name !== 'AbortError') {
      console.error(error);
      status('Project load failed', 'error');
      alert(error.message);
    }
  }
}

async function autoLoad() {
  status('Loading');
  if (location.protocol === 'http:' || location.protocol === 'https:') {
    try {
      await loadFromApi();
      return;
    } catch (error) {
      console.error(error);
    }
  }
  if (!window.showDirectoryPicker) {
    status('Open through start.ps1', 'error');
    return;
  }
  try {
    const root = await storedHandle();
    if (root && await root.queryPermission({ mode: 'readwrite' }) === 'granted') {
      await loadFromFolder(root);
      return;
    }
  } catch (error) {
    console.error(error);
  }
  ui.open.style.display = '';
  status('Select the project once', 'dirty');
}

function overrideFor(id) {
  return state.overrides[id] || null;
}

function ensureOverride(id) {
  return state.overrides[id] ||= {};
}

function pruneOverride(id) {
  const value = state.overrides[id];
  if (!value) return;
  if (value.options && Object.keys(value.options).length === 0) delete value.options;
  if (Object.keys(value).length === 0) delete state.overrides[id];
}

function effectiveOptions(skill) {
  return { ...(skill.options || {}), ...(overrideFor(skill.id)?.options || {}) };
}

function formatToken(value, formatter) {
  if (formatter === 'percent') return `${Math.round(Number(value) * 10000) / 100}%`;
  if (formatter === 'seconds') return `${value} seconds`;
  if (formatter === 'multiplier') return `${value}x`;
  if (formatter === 'currency') return `$${value}`;
  return String(value ?? '');
}

function descriptionFor(skill) {
  const options = effectiveOptions(skill);
  return String(state.drafts[skill.id] || '').replace(/\{\{([A-Za-z0-9_]+)(?:\|([A-Za-z0-9_]+))?\}\}/g, (token, key, formatter) => {
    return key in options ? formatToken(options[key], formatter || 'raw') : token;
  });
}

function setMeta(skill, key, value) {
  const entry = ensureOverride(skill.id);
  if (value === skill.metadata[key]) delete entry[key];
  else entry[key] = value;
  pruneOverride(skill.id);
  markDirty();
}

function setOption(skill, key, value) {
  const entry = ensureOverride(skill.id);
  entry.options ||= {};
  if (value === skill.options[key]) delete entry.options[key];
  else entry.options[key] = value;
  pruneOverride(skill.id);
  state.localization[`${skill.id}_desc`] = descriptionFor(skill);
  markDirty();
  const preview = document.getElementById(`preview_${skill.id}`);
  if (preview) preview.textContent = descriptionFor(skill);
}

function field(label, control) {
  const wrapper = document.createElement('div');
  wrapper.className = 'field';
  const caption = document.createElement('label');
  caption.textContent = label;
  wrapper.append(caption, control);
  return wrapper;
}

function textField(label, value, callback, area = false) {
  const control = document.createElement(area ? 'textarea' : 'input');
  control.value = value ?? '';
  control.addEventListener('input', () => callback(control.value));
  return field(label, control);
}

function numberField(label, value, callback) {
  const control = document.createElement('input');
  control.type = 'number';
  control.step = 'any';
  control.value = value ?? 0;
  control.addEventListener('input', () => callback(Number(control.value)));
  return field(label, control);
}

function selectField(label, value, options, callback) {
  const control = document.createElement('select');
  for (const option of options) control.add(new Option(option, option));
  control.value = String(value);
  control.addEventListener('change', () => callback(control.value));
  return field(label, control);
}

function checkField(label, value, callback) {
  const control = document.createElement('input');
  control.type = 'checkbox';
  control.checked = Boolean(value);
  control.addEventListener('change', () => callback(control.checked));
  const wrapper = document.createElement('label');
  wrapper.className = 'toggle';
  wrapper.append(control, document.createTextNode(label));
  const host = document.createElement('div');
  host.className = 'field';
  host.appendChild(wrapper);
  return host;
}

function resetSkill(skill) {
  delete state.overrides[skill.id];
  state.localization[skill.id] = skill.displayName;
  state.drafts[skill.id] = state.defaultBindings[skill.id] || skill.description || '';
  if (state.defaultBindings[skill.id]) state.bindings[skill.id] = state.defaultBindings[skill.id];
  else delete state.bindings[skill.id];
  state.localization[`${skill.id}_desc`] = descriptionFor(skill);
  markDirty();
  render();
}

function renderCard(skill) {
  const current = overrideFor(skill.id) || {};
  const card = document.createElement('article');
  card.className = `card${overrideFor(skill.id) ? ' overridden' : ''}`;
  const head = document.createElement('div');
  head.className = 'card-head';
  const swatch = document.createElement('span');
  swatch.className = 'swatch';
  swatch.style.background = current.color ?? skill.metadata.color;
  const title = document.createElement('span');
  title.className = 'card-title';
  title.textContent = state.localization[skill.id] || skill.displayName;
  const rarity = current.rarity ?? skill.metadata.rarity;
  const badge = document.createElement('span');
  badge.className = `badge ${String(rarity).toLowerCase()}`;
  badge.textContent = rarity;
  head.append(swatch, title, badge);
  card.appendChild(head);

  card.appendChild(textField('Display name', title.textContent, value => {
    state.localization[skill.id] = value;
    title.textContent = value;
    markDirty();
  }));

  const description = textField('Description or template', state.drafts[skill.id], value => {
    state.drafts[skill.id] = value;
    if (value.includes('{{')) state.bindings[skill.id] = value;
    else delete state.bindings[skill.id];
    state.localization[`${skill.id}_desc`] = descriptionFor(skill);
    markDirty();
    document.getElementById(`preview_${skill.id}`).textContent = descriptionFor(skill);
  }, true);
  const preview = document.createElement('div');
  preview.id = `preview_${skill.id}`;
  preview.className = 'description-preview';
  preview.textContent = descriptionFor(skill);
  description.appendChild(preview);
  card.appendChild(description);

  const first = document.createElement('div');
  first.className = 'row';
  first.append(
    textField('Color', current.color ?? skill.metadata.color, value => setMeta(skill, 'color', value)),
    selectField('Rarity', rarity, ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary'], value => setMeta(skill, 'rarity', value)),
  );
  card.appendChild(first);

  const second = document.createElement('div');
  second.className = 'row';
  second.append(
    selectField('Only team', current.onlyTeam ?? skill.metadata.onlyTeam, ['None', 'Terrorist', 'CounterTerrorist'], value => setMeta(skill, 'onlyTeam', value)),
    numberField('Max per server', current.maxPerServer ?? skill.metadata.maxPerServer, value => setMeta(skill, 'maxPerServer', value)),
  );
  card.appendChild(second);

  const third = document.createElement('div');
  third.className = 'row';
  third.append(
    checkField('Active', current.enabled ?? skill.metadata.active, value => {
      const entry = ensureOverride(skill.id);
      if (value === skill.metadata.active) delete entry.enabled;
      else entry.enabled = value;
      pruneOverride(skill.id);
      markDirty();
    }),
    checkField('Disable on freeze', current.disableOnFreezeTime ?? skill.metadata.disableOnFreezeTime, value => setMeta(skill, 'disableOnFreezeTime', value)),
    checkField('Needs teammates', current.needsTeammates ?? skill.metadata.needsTeammates, value => setMeta(skill, 'needsTeammates', value)),
  );
  card.appendChild(third);

  const options = document.createElement('div');
  options.className = 'options';
  const heading = document.createElement('div');
  heading.className = 'options-title';
  heading.textContent = `Skill options, ${skill.className}Options`;
  options.appendChild(heading);
  const keys = Object.keys(skill.options || {});
  if (keys.length === 0) {
    const empty = document.createElement('div');
    empty.className = 'muted';
    empty.textContent = 'No tunable options';
    options.appendChild(empty);
  } else {
    const grid = document.createElement('div');
    grid.className = 'options-grid';
    for (const key of keys) {
      const original = skill.options[key];
      const value = current.options && key in current.options ? current.options[key] : original;
      if (typeof original === 'boolean') {
        grid.appendChild(selectField(key, String(value), ['true', 'false'], raw => setOption(skill, key, raw === 'true')));
      } else if (typeof original === 'number') {
        grid.appendChild(numberField(key, value, next => setOption(skill, key, next)));
      } else {
        grid.appendChild(textField(key, value, next => setOption(skill, key, next)));
      }
    }
    options.appendChild(grid);
  }
  card.appendChild(options);

  const footer = document.createElement('div');
  footer.className = 'card-footer';
  const id = document.createElement('span');
  id.className = 'muted';
  id.textContent = skill.id;
  const reset = document.createElement('button');
  reset.className = 'reset-card';
  reset.textContent = 'Reset skill';
  reset.addEventListener('click', () => resetSkill(skill));
  footer.append(id, reset);
  card.appendChild(footer);
  return card;
}

function render() {
  const query = ui.search.value.trim().toLowerCase();
  const rarity = ui.rarity.value;
  const onlyOverrides = ui.overridden.checked;
  const filtered = state.skills.filter(skill => {
    const text = `${skill.id} ${state.localization[skill.id] || skill.displayName} ${descriptionFor(skill)}`.toLowerCase();
    if (query && !text.includes(query)) return false;
    if (rarity && (overrideFor(skill.id)?.rarity ?? skill.metadata.rarity) !== rarity) return false;
    return !onlyOverrides || Boolean(overrideFor(skill.id));
  });
  ui.grid.replaceChildren(...filtered.map(renderCard));
  ui.count.textContent = `${filtered.length} of ${state.skills.length}, ${Object.keys(state.overrides).length} overridden`;
}

function payload() {
  for (const skill of state.skills) state.localization[`${skill.id}_desc`] = descriptionFor(skill);
  state.config.skills = state.overrides;
  return { localization: state.localization, config: state.config, bindings: state.bindings };
}

async function saveAll() {
  status('Saving');
  try {
    const value = payload();
    if (state.mode === 'api') {
      const response = await fetch('/api/save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-HeroEditor-Token': state.sessionToken },
        body: JSON.stringify(value),
      });
      const result = await response.json();
      if (!response.ok || !result.ok) throw new Error(result.error || 'Save failed');
      state.nextVersion = result.nextVersion || state.nextVersion;
    } else {
      await writeJson(state.handles.localization, value.localization);
      await writeJson(state.handles.config, value.config);
      await writeJson(state.handles.bindings, value.bindings);
    }
    state.dirty = false;
    status('Saved', 'saved');
    updateControls();
  } catch (error) {
    console.error(error);
    status('Save failed', 'error');
    alert(error.message);
  }
}

function resetAll() {
  if (!confirm('Reset every skill to the generated code defaults and default descriptions?')) return;
  state.overrides = {};
  state.config.skills = state.overrides;
  state.bindings = clone(state.defaultBindings);
  for (const skill of state.skills) {
    state.localization[skill.id] = skill.displayName;
    state.drafts[skill.id] = state.defaultBindings[skill.id] || skill.description || '';
    state.localization[`${skill.id}_desc`] = descriptionFor(skill);
  }
  markDirty();
  render();
}

async function publishLocal() {
  if (!confirm(`Build local HeroShift version ${state.nextVersion} without creating a tag or GitHub release?`)) return;
  state.busy = true;
  updateControls();
  status(`Building ${state.nextVersion}`);
  try {
    const response = await fetch('/api/publish', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-HeroEditor-Token': state.sessionToken },
      body: JSON.stringify(payload()),
    });
    const result = await response.json();
    if (!response.ok || !result.ok) throw new Error(result.error || 'Packaging failed');
    state.dirty = false;
    const parts = result.version.split('.').map(Number);
    parts[2] += 1;
    state.nextVersion = parts.join('.');
    status(`Created v${result.version}`, 'saved');
    showResult(`HeroShift v${result.version}`, `Archive: ${result.archive}\n\n${result.output}`);
  } catch (error) {
    console.error(error);
    status('Packaging failed', 'error');
    showResult('Packaging failed', error.message);
  } finally {
    state.busy = false;
    updateControls();
  }
}

ui.open.addEventListener('click', chooseFolder);
ui.save.addEventListener('click', saveAll);
ui.reset.addEventListener('click', resetAll);
ui.publish.addEventListener('click', publishLocal);
ui.search.addEventListener('input', render);
ui.rarity.addEventListener('change', render);
ui.overridden.addEventListener('change', render);
$('#closeModalBtn').addEventListener('click', () => ui.modal.classList.remove('visible'));
ui.modal.addEventListener('click', event => {
  if (event.target === ui.modal) ui.modal.classList.remove('visible');
});
window.addEventListener('beforeunload', event => {
  if (state.dirty) {
    event.preventDefault();
    event.returnValue = '';
  }
});

updateControls();
autoLoad();
