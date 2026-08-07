import React from 'react';
import styles from './styles.module.css';

/**
 * WiringDiagram
 *
 * A theme-aware SVG diagram of a typical QuestNav robot wiring layout:
 *
 *   Quest headset --USB-C--> USB-C Ethernet adapter --Ethernet--> [switch] --> radio --> roboRIO
 *   USB battery bank --USB-A to USB-C (5 V)--> adapter power input
 *
 * Colors reference Infima CSS variables (see styles.module.css) so the diagram
 * adapts automatically to Docusaurus light and dark mode.
 */
export default function WiringDiagram(): React.ReactElement {
  return (
    <div className={styles.diagram}>
      <svg
        viewBox="0 0 960 430"
        role="img"
        aria-label="QuestNav wiring diagram: the Quest headset connects over USB-C to a USB-C to Ethernet adapter. The adapter's Ethernet output runs through an optional Ethernet switch to the robot radio and then the roboRIO. A USB battery bank powers the adapter over a USB-A to USB-C cable at 5 volts."
      >
        <title>QuestNav wiring diagram</title>

        <defs>
          <marker
            id="wd-arrow-data"
            markerWidth="9"
            markerHeight="9"
            refX="7"
            refY="4"
            orient="auto"
          >
            <path className={styles.arrowData} d="M0,0 L8,4 L0,8 Z" />
          </marker>
          <marker
            id="wd-arrow-power"
            markerWidth="9"
            markerHeight="9"
            refX="7"
            refY="4"
            orient="auto"
          >
            <path className={styles.arrowPower} d="M0,0 L8,4 L0,8 Z" />
          </marker>
        </defs>

        {/* ---- Data (Ethernet / USB-C) connections ---- */}
        <line className={styles.dataLine} x1="155" y1="106" x2="212" y2="106" markerEnd="url(#wd-arrow-data)" />
        <line className={styles.dataLine} x1="355" y1="106" x2="412" y2="106" markerEnd="url(#wd-arrow-data)" />
        <line className={styles.dataLine} x1="555" y1="106" x2="612" y2="106" markerEnd="url(#wd-arrow-data)" />
        <line className={styles.dataLine} x1="745" y1="106" x2="797" y2="106" markerEnd="url(#wd-arrow-data)" />

        <text className={styles.lineLabel} x="184" y="92">USB-C</text>
        <text className={styles.lineLabel} x="385" y="92">Ethernet</text>
        <text className={styles.lineLabel} x="585" y="92">Ethernet</text>
        <text className={styles.lineLabel} x="771" y="92">Ethernet</text>

        {/* ---- Power connection ---- */}
        <line className={styles.powerLine} x1="285" y1="296" x2="285" y2="151" markerEnd="url(#wd-arrow-power)" />
        <text className={styles.lineLabelStart} x="300" y="212">USB-A → USB-C</text>
        <text className={styles.lineLabelStart} x="300" y="230">5 V power</text>

        {/* ---- Component boxes ---- */}
        {/* Quest headset */}
        <rect className={styles.box} x="15" y="64" width="140" height="84" rx="8" />
        <text className={styles.label} x="85" y="100">Quest</text>
        <text className={styles.sublabel} x="85" y="120">3 / 3S Headset</text>

        {/* USB-C Ethernet adapter */}
        <rect className={styles.box} x="215" y="64" width="140" height="84" rx="8" />
        <text className={styles.label} x="285" y="100">USB-C</text>
        <text className={styles.sublabel} x="285" y="120">Ethernet Adapter</text>

        {/* Ethernet switch (optional) */}
        <rect className={styles.boxOptional} x="415" y="64" width="140" height="84" rx="8" />
        <text className={styles.label} x="485" y="100">Ethernet Switch</text>
        <text className={styles.sublabel} x="485" y="120">(optional)</text>

        {/* Robot radio */}
        <rect className={styles.box} x="615" y="64" width="130" height="84" rx="8" />
        <text className={styles.label} x="680" y="111">Robot Radio</text>

        {/* roboRIO */}
        <rect className={styles.box} x="800" y="64" width="145" height="84" rx="8" />
        <text className={styles.label} x="872" y="111">roboRIO</text>

        {/* USB battery bank */}
        <rect className={styles.box} x="215" y="296" width="140" height="80" rx="8" />
        <text className={styles.label} x="285" y="331">USB Battery</text>
        <text className={styles.label} x="285" y="351">Bank</text>

        {/* ---- Legend ---- */}
        <line className={styles.dataLine} x1="305" y1="402" x2="337" y2="402" />
        <text className={styles.lineLabelStart} x="345" y="406">Ethernet / USB-C (data)</text>
        <line className={styles.powerLine} x1="560" y1="402" x2="592" y2="402" />
        <text className={styles.lineLabelStart} x="600" y="406">USB power</text>
      </svg>
    </div>
  );
}
