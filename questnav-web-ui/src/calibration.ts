
// Utility types and functions for calibration calculations
import { levenbergMarquardt } from '@mytskine/curve-fitting'

// Local point type for this module (do not export the component's Sample type)
export type Point2D = { x: number; y: number }

/**
 * Fit a circle to the given points using Levenberg–Marquardt nonlinear regression.
 * Model: (x - h)^2 + (y - k)^2 ≈ r^2
 * Returns the estimated center (h, k) and radius r.
 */
export function fit_circle(
    points: ReadonlyArray<Point2D>
): Point2D {
    const n = points.length
    if (n === 0) return { x: 0, y: 0 }
    if (n === 1) return points[0]

    // Initial guess: center at geometric median, radius as mean distance
    // Start from arithmetic mean for faster convergence
    let x = points.reduce((s, p) => s + p.x, 0) / points.length
    let y = points.reduce((s, p) => s + p.y, 0) / points.length
    const c0 = {x: x, y: y}
    const r0 = points.reduce((s, p) => s + Math.hypot(p.x - c0.x, p.y - c0.y), 0) / n

    // The LM library expects data {x: number[], y: number[]} and a model f(params)(x) -> y
    // We'll pass indices as x and close over the points array to compute residuals to zero.
    const xData = Array.from({ length: n }, (_, i) => i)
    const yData = new Array(n).fill(0)

    const model = ([h, k, r]: [number, number, number]) => (i: number) => {
        const p = points[i]
        const dx = p.x - h
        const dy = p.y - k
        return dx * dx + dy * dy - r * r
    }

    const result = levenbergMarquardt(
        { x: xData, y: yData },
        model,
        {
            parameters: {
                initial: [c0.x, c0.y, Math.max(r0, 1e-6)]
            }
        }
    ) as unknown as { parameters: [number, number, number] }

    const [h, k, r] = result.parameters
    return { x: h, y: k }
}