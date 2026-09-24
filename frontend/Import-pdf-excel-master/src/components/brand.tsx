'use client';

import Image from 'next/image';
import logoLight from '@/assets/logo white mode.png';
import logoDark from '@/assets/logo dark mode.png';
import symbolLight from '@/assets/slogo white mode.png';
import symbolDark from '@/assets/slogo dark mode.png';

/** Both variants are rendered so the theme switches without a loading flash. */
export function Brand({ compact = false }: { compact?: boolean }) {
  return <span className={`brand-art ${compact ? 'brand-art-compact' : ''}`}>
    <Image src={compact ? symbolLight : logoLight} alt="ALIAS CONVERTPDF" className="brand-light" sizes={compact ? '110px' : '200px'} priority />
    <Image src={compact ? symbolDark : logoDark} alt="ALIAS CONVERTPDF" className="brand-dark" sizes={compact ? '110px' : '200px'} priority />
  </span>;
}
