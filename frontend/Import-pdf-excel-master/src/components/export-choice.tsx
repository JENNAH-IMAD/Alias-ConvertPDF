'use client';
import { Check, FileOutput } from 'lucide-react';
import Link from 'next/link';
import { Template, templateLabel } from '@/services/statements';

export function ExportChoice({ templates, value, onChange }: { templates: Template[]; value: string; onChange: (id: string) => void }) {
  const selected = templates.find(t => t.id === value);
  return <section id="export-choice" className="ui-card scroll-mt-24">
    <p className="ui-eyebrow">ÉTAPE 2 · FORMAT DE SORTIE</p><h2 className="text-xl font-semibold mt-2">Quel fichier souhaitez-vous obtenir ?</h2>
    <p className="ui-description">Choisissez votre destination après l’import. Vous pourrez encore la changer avant la génération finale.</p>
    <fieldset className="export-choice-grid mt-5"><legend className="sr-only">Type de fichier final</legend>
      {(['SAGE100_STANDARD', 'SAGE_X3', 'CUSTOM_CSV'] as const).map(type => {
        const available = templates.filter(t => t.type === type && t.isActive);
        const preset = available.find(t => t.code === type) || available[0];
        return <label key={type} className={`export-choice ${selected?.type === type ? 'selected' : ''} ${!preset ? 'unavailable' : ''}`}>
          <input className="sr-only" type="radio" name="export-kind" value={type} checked={selected?.type === type} disabled={!preset} onChange={() => onChange(preset.id)}/>
          <span className="export-choice-icon"><FileOutput size={22}/>{selected?.type === type && <Check size={16}/>}</span>
          <strong>{templateLabel[type]}</strong><span className="muted text-xs">{type === 'SAGE100_STANDARD' ? 'Fichier TXT · import comptable' : type === 'SAGE_X3' ? 'Fichier CSV · import banque' : 'Fichier CSV · colonnes personnalisées'}</span>
          {!preset && <span className="text-xs">Aucun modèle actif</span>}
        </label>;
      })}
    </fieldset>
    {selected && <label className="block mt-5 text-sm">Modèle d’export<select className="ui-input mt-2" value={value} onChange={e => onChange(e.target.value)}>{templates.filter(t => t.isActive && t.type === selected.type).map(t => <option key={t.id} value={t.id}>{t.name} · {t.encoding}</option>)}</select></label>}
    {!templates.some(t => t.isActive) && <p className="ui-description mt-3">Demandez à un administrateur d’activer un modèle dans <Link className="accent-text underline" href="/templates">Modèles d’export</Link>.</p>}
  </section>;
}
