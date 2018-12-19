// Variant of a Lambert solver based on Battin's method.
// Released under the GNU GENERAL PUBLIC LICENSE as part of the PyKEP library:
/*****************************************************************************
 *   Copyright (C) 2004-2018 The pykep development team,                     *
 *   Advanced Concepts Team (ACT), European Space Agency (ESA)               *
 *                                                                           *
 *   https://gitter.im/esa/pykep                                             *
 *   https://github.com/esa/pykep                                            *
 *                                                                           *
 *   act@esa.int                                                             *
 *                                                                           *
 *   This program is free software; you can redistribute it and/or modify    *
 *   it under the terms of the GNU General Public License as published by    *
 *   the Free Software Foundation; either version 2 of the License, or       *
 *   (at your option) any later version.                                     *
 *                                                                           *
 *   This program is distributed in the hope that it will be useful,         *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of          *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the           *
 *   GNU General Public License for more details.                            *
 *                                                                           *
 *   You should have received a copy of the GNU General Public License       *
 *   along with this program; if not, write to the                           *
 *   Free Software Foundation, Inc.,                                         *
 *   59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.               *
 *****************************************************************************/


using System;
using kOSMainframe.ExtraMath;

namespace kOSMainframe.Orbital {
    public class LambertBattinSolver {
        static double tof_curve()
        /// <summary>
        /// Convert the Battin's variable x to time of flight in a non dimensional Lambert's problem.
        /// </summary>
        /// <returns>The time-of flight from the Battin's variable x.</returns>
        /// <param name="x">Battin's variable.</param>
        /// <param name="s">Half perimeter of the triangle defined by r1,r2.</param>
        /// <param name="c">Chord defined by the r1,r2 triangle.</param>
        /// <param name="lw">Switch between long and short way solutions.</param>
        /// <param name="N">Multiple revolutions.</param>
        static double x2tof(double x, double s, double c, bool lw, int N = 0) {
            double am, a, alfa, beta;

            am = s / 2;
            a = am / (1 - x * x);
            if (x < 1) { // ellipse
                beta = 2 * Math.Asin(Math.Sqrt((s - c) / (2 * a)));
                if (lw) beta = -beta;
                alfa = 2 * Math.Acos(x);
            } else {
                alfa = 2 * Functions.Acosh(x);
                beta = 2 * Functions.Asinh(Math.Sqrt((s - c) / (-2 * a)));
                if (lw) beta = -beta;
            }

            if (a > 0) {
                return (a * Math.Sqrt(a) * ((alfa - Math.Sin(alfa)) - (beta - Math.Sin(beta)) + 2 * Math.PI * N));
            } else {
                return (-a * Math.Sqrt(-a) * ((Math.Sinh(alfa) - alfa) - (Math.Sinh(beta) - beta)));
            }
        }
    }
}
