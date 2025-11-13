#!/usr/bin/env python3
"""
QuestNav Viewer - Complete Feature Demonstration

Demonstrates ALL features of the questnav-lib Python implementation:
- Real-time pose tracking with frame rate display
- Device status monitoring (battery, tracking state)
- Connection quality metrics (latency, frame drops)
- Pose reset commands with feedback
- Command response handling
- Complete diagnostic information

This viewer acts as a RoboRIO (NT4 server) and uses the questnav_lib
exactly as robot code would.

Usage:
    python questnav_viewer.py [--port PORT]
"""

import argparse
import sys
import time
import tkinter as tk
from tkinter import ttk, scrolledtext, messagebox
from datetime import datetime
import math

try:
    import ntcore
    from wpimath.geometry import Pose3d, Translation3d, Rotation3d
    
    # Import QuestNav library
    from questnav import QuestNav, PoseFrame
    
except ImportError as e:
    print(f"ERROR: {e}")
    print("Install dependencies: pip install -r requirements.txt")
    import traceback
    traceback.print_exc()
    sys.exit(1)


class QuestNavViewer:
    """
    Complete QuestNav viewer demonstrating all questnav-lib features.
    
    Implements:
    - All data retrieval methods
    - All status methods
    - Command sending with response handling
    - Comprehensive diagnostics
    """
    
    def __init__(self):
        # Start NT4 server (on a real robot, RoboRIO does this automatically)
        self.inst = ntcore.NetworkTableInstance.getDefault()
        self.inst.startServer(listen_address="", port4=5810)
        
        # Add connection listener for debugging
        self.conn_listener = ntcore.NetworkTableListenerPoller(self.inst)
        self.conn_listener.addConnectionListener(True)
        
        # Create QuestNav instance (demonstrates library usage)
        self.questnav = QuestNav()
        
        # Tracking variables for diagnostics
        self.total_frames_received = 0
        self.last_frame_count = 0
        self.frame_drops = 0
        self.start_time = time.time()
        self.frame_rate_samples = []
        self.last_frame_time = 0
        
        # Log control
        self.log_paused = False
        
        # GUI setup
        self.root = tk.Tk()
        self.root.title("QuestNav Viewer - Complete Feature Demo")
        self.root.geometry("900x700")
        self.root.minsize(800, 600)  # Set minimum window size
        
        self._setup_ui()
        self._running = True
        
        # No console output - everything shows in GUI log
        
        self._update_loop()
    
    def _setup_ui(self):
        """Setup comprehensive UI showing all features"""
        
        # === CONNECTION STATUS ===
        status_frame = ttk.LabelFrame(self.root, text="Connection Status", padding=10)
        status_frame.pack(fill=tk.X, padx=10, pady=5)
        
        status_grid = ttk.Frame(status_frame)
        status_grid.pack()
        
        self.conn_label = ttk.Label(status_grid, text="[ ] Disconnected", 
                                     font=("Arial", 11, "bold"))
        self.conn_label.grid(row=0, column=0, padx=10)
        
        self.track_label = ttk.Label(status_grid, text="[ ] Not Tracking")
        self.track_label.grid(row=0, column=1, padx=10)
        
        self.batt_label = ttk.Label(status_grid, text="Battery: ---%")
        self.batt_label.grid(row=0, column=2, padx=10)
        
        self.latency_label = ttk.Label(status_grid, text="Latency: --- ms")
        self.latency_label.grid(row=0, column=3, padx=10)
        
        # === POSE DATA ===
        pose_frame = ttk.LabelFrame(self.root, text="Pose Data (Quest Position)", padding=10)
        pose_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        pose_grid = ttk.Frame(pose_frame)
        pose_grid.pack(fill=tk.BOTH, expand=True)
        
        # Position column
        ttk.Label(pose_grid, text="Position (meters):", font=("Arial", 10, "bold")).grid(
            row=0, column=0, sticky=tk.W, pady=5)
        self.pos_x = ttk.Label(pose_grid, text="X: ---", font=("Courier", 10))
        self.pos_x.grid(row=1, column=0, sticky=tk.W, padx=20)
        self.pos_y = ttk.Label(pose_grid, text="Y: ---", font=("Courier", 10))
        self.pos_y.grid(row=2, column=0, sticky=tk.W, padx=20)
        self.pos_z = ttk.Label(pose_grid, text="Z: ---", font=("Courier", 10))
        self.pos_z.grid(row=3, column=0, sticky=tk.W, padx=20)
        
        # Rotation column
        ttk.Label(pose_grid, text="Rotation (degrees):", font=("Arial", 10, "bold")).grid(
            row=0, column=1, sticky=tk.W, padx=20, pady=5)
        self.rot_r = ttk.Label(pose_grid, text="Roll:  ---", font=("Courier", 10))
        self.rot_r.grid(row=1, column=1, sticky=tk.W, padx=20)
        self.rot_p = ttk.Label(pose_grid, text="Pitch: ---", font=("Courier", 10))
        self.rot_p.grid(row=2, column=1, sticky=tk.W, padx=20)
        self.rot_y = ttk.Label(pose_grid, text="Yaw:   ---", font=("Courier", 10))
        self.rot_y.grid(row=3, column=1, sticky=tk.W, padx=20)
        
        # === DIAGNOSTICS ===
        diag_frame = ttk.LabelFrame(self.root, text="Diagnostics & Statistics", padding=10)
        diag_frame.pack(fill=tk.X, padx=10, pady=5)
        
        diag_grid = ttk.Frame(diag_frame)
        diag_grid.pack()
        
        # Row 1
        self.frame_count_label = ttk.Label(diag_grid, text="Frame Count: 0")
        self.frame_count_label.grid(row=0, column=0, sticky=tk.W, padx=10, pady=2)
        
        self.frame_rate_label = ttk.Label(diag_grid, text="Frame Rate: --- Hz")
        self.frame_rate_label.grid(row=0, column=1, sticky=tk.W, padx=10, pady=2)
        
        # Row 2
        self.total_frames_label = ttk.Label(diag_grid, text="Total Received: 0")
        self.total_frames_label.grid(row=1, column=0, sticky=tk.W, padx=10, pady=2)
        
        self.frame_drops_label = ttk.Label(diag_grid, text="Frame Drops: 0")
        self.frame_drops_label.grid(row=1, column=1, sticky=tk.W, padx=10, pady=2)
        
        # Row 3
        self.tracking_lost_label = ttk.Label(diag_grid, text="Tracking Lost Events: 0")
        self.tracking_lost_label.grid(row=2, column=0, sticky=tk.W, padx=10, pady=2)
        
        self.uptime_label = ttk.Label(diag_grid, text="Uptime: 0:00")
        self.uptime_label.grid(row=2, column=1, sticky=tk.W, padx=10, pady=2)
        
        # === CONTROLS ===
        control_frame = ttk.LabelFrame(self.root, text="Commands", padding=10)
        control_frame.pack(fill=tk.X, padx=10, pady=5)
        
        btn_frame = ttk.Frame(control_frame)
        btn_frame.pack()
        
        ttk.Button(btn_frame, text="Reset Pose to Origin (0,0,0)", 
                  command=self._reset_pose_origin).grid(row=0, column=0, padx=5, pady=5)
        
        ttk.Button(btn_frame, text="Reset Pose to Custom...", 
                  command=self._reset_pose_custom).grid(row=0, column=1, padx=5, pady=5)
        
        ttk.Button(btn_frame, text="Clear Log", 
                  command=self._clear_log).grid(row=0, column=2, padx=5, pady=5)
        
        self.pause_log_btn = ttk.Button(btn_frame, text="Pause Log", 
                  command=self._toggle_log_pause)
        self.pause_log_btn.grid(row=0, column=3, padx=5, pady=5)
        
        ttk.Button(btn_frame, text="Export Log...", 
                  command=self._export_log).grid(row=0, column=4, padx=5, pady=5)
        
        # === EVENT LOG ===
        log_frame = ttk.LabelFrame(self.root, text="Event Log", padding=10)
        log_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        self.log = scrolledtext.ScrolledText(log_frame, height=10, state='disabled', 
                                             font=("Courier", 9))
        self.log.pack(fill=tk.BOTH, expand=True)
        
        self._log("QuestNav Viewer initialized")
        self._log("NT4 Server listening on port 5810")
        self._log("Waiting for Quest headset...")
    
    def _update_loop(self):
        """Main update loop - demonstrates library usage"""
        if not self._running:
            return
        
        current_time = time.time()
        
        # Check for connection events (for logging)
        for event in self.conn_listener.readQueue():
            if event.is_(ntcore.EventFlags.kConnected):
                conn_info = event.data
                self._log(f"Quest connected from {conn_info.remote_ip}")
            elif event.is_(ntcore.EventFlags.kDisconnected):
                self._log("Quest disconnected")
        
        # === DEMONSTRATE command_periodic() ===
        # This processes command responses (required in robot code)
        self.questnav.command_periodic()
        
        # === DEMONSTRATE get_all_unread_pose_frames() ===
        # Primary method for getting Quest pose data
        frames = self.questnav.get_all_unread_pose_frames()
        
        # Process each frame (like robot code would)
        for frame in frames:
            self.total_frames_received += 1
            
            # Detect frame drops (only check first frame in batch)
            if self.last_frame_count > 0 and frame == frames[0]:
                expected = self.last_frame_count + 1
                if frame.frame_count > expected:
                    drops = frame.frame_count - expected
                    self.frame_drops += drops
                    if drops > 5:  # Only log significant drops
                        self._log(f"[WARN] Dropped {drops} frames")
            
            self.last_frame_count = frame.frame_count
            
            # Calculate frame rate
            if self.last_frame_time > 0:
                dt = current_time - self.last_frame_time
                if dt > 0:
                    fps = 1.0 / dt
                    self.frame_rate_samples.append(fps)
                    if len(self.frame_rate_samples) > 10:
                        self.frame_rate_samples.pop(0)
            
            self.last_frame_time = current_time
            
            # Update pose display with latest frame
            self._update_pose_display(frame.quest_pose_3d)
        
        # === DEMONSTRATE is_connected() ===
        connected = self.questnav.is_connected()
        if connected:
            self.conn_label.config(text="[X] Connected", foreground="green")
        else:
            self.conn_label.config(text="[ ] Disconnected", foreground="red")
        
        # === DEMONSTRATE is_tracking() ===
        tracking = self.questnav.is_tracking()
        if tracking:
            self.track_label.config(text="[X] Tracking", foreground="green")
        else:
            self.track_label.config(text="[ ] Not Tracking", foreground="red")
        
        # === DEMONSTRATE get_battery_percent() ===
        battery = self.questnav.get_battery_percent()
        if battery is not None:
            self.batt_label.config(text=f"Battery: {battery}%")
            # Warn on low battery
            if battery < 20 and not hasattr(self, '_low_battery_warned'):
                self._log(f"[WARN] Low battery: {battery}%")
                self._low_battery_warned = True
        
        # === DEMONSTRATE get_latency() ===
        latency = self.questnav.get_latency()
        self.latency_label.config(text=f"Latency: {latency:.1f} ms")
        
        # === DEMONSTRATE get_frame_count() ===
        frame_count = self.questnav.get_frame_count()
        if frame_count is not None:
            self.frame_count_label.config(text=f"Frame Count: {frame_count}")
        
        # === DEMONSTRATE get_tracking_lost_counter() ===
        tracking_lost = self.questnav.get_tracking_lost_counter()
        if tracking_lost is not None:
            self.tracking_lost_label.config(text=f"Tracking Lost Events: {tracking_lost}")
        
        # Update other stats
        self.total_frames_label.config(text=f"Total Received: {self.total_frames_received}")
        self.frame_drops_label.config(text=f"Frame Drops: {self.frame_drops}")
        
        # Calculate average frame rate
        if self.frame_rate_samples:
            avg_fps = sum(self.frame_rate_samples) / len(self.frame_rate_samples)
            self.frame_rate_label.config(text=f"Frame Rate: {avg_fps:.1f} Hz")
        
        # Update uptime
        uptime_seconds = int(current_time - self.start_time)
        uptime_mins = uptime_seconds // 60
        uptime_secs = uptime_seconds % 60
        self.uptime_label.config(text=f"Uptime: {uptime_mins}:{uptime_secs:02d}")
        
        # Log connection state changes
        if hasattr(self, '_was_connected'):
            if connected and not self._was_connected:
                self._log("[OK] Quest connected - receiving data")
            elif not connected and self._was_connected:
                self._log("[WARN] Quest disconnected")
        self._was_connected = connected
        
        # Log tracking state changes
        if hasattr(self, '_was_tracking'):
            if tracking and not self._was_tracking:
                self._log("[OK] Tracking started")
            elif not tracking and self._was_tracking:
                self._log("[WARN] Tracking lost!")
        self._was_tracking = tracking
        
        # Schedule next update (50ms = 20 Hz)
        self.root.after(50, self._update_loop)
    
    def _update_pose_display(self, pose: Pose3d):
        """Update pose display from Pose3d"""
        t = pose.translation()
        r = pose.rotation()
        
        # Update position
        self.pos_x.config(text=f"X: {t.X():>8.3f} m")
        self.pos_y.config(text=f"Y: {t.Y():>8.3f} m")
        self.pos_z.config(text=f"Z: {t.Z():>8.3f} m")
        
        # Update rotation (convert to degrees)
        self.rot_r.config(text=f"Roll:  {math.degrees(r.X()):>7.1f}°")
        self.rot_p.config(text=f"Pitch: {math.degrees(r.Y()):>7.1f}°")
        self.rot_y.config(text=f"Yaw:   {math.degrees(r.Z()):>7.1f}°")
    
    def _reset_pose_origin(self):
        """Demonstrate set_pose() - Reset to origin"""
        try:
            # Create origin pose (0, 0, 0)
            origin_pose = Pose3d()
            
            # Send pose reset command (demonstrates set_pose method)
            self.questnav.set_pose(origin_pose)
            
            self._log("[CMD] Sent pose reset to origin (0, 0, 0)")
            self._log("      Waiting for command response...")
            
        except Exception as e:
            self._log(f"[ERROR] Failed to send pose reset: {e}")
    
    def _reset_pose_custom(self):
        """Demonstrate set_pose() with custom pose"""
        # Create custom pose dialog
        dialog = tk.Toplevel(self.root)
        dialog.title("Custom Pose Reset")
        dialog.geometry("400x300")
        dialog.minsize(400, 300)
        dialog.transient(self.root)
        dialog.grab_set()  # Make it modal
        
        ttk.Label(dialog, text="Enter Target Pose:", font=("Arial", 11, "bold")).pack(pady=10)
        
        # Input frame
        input_frame = ttk.Frame(dialog, padding=20)
        input_frame.pack()
        
        ttk.Label(input_frame, text="X (m):").grid(row=0, column=0, sticky=tk.W, pady=5)
        x_entry = ttk.Entry(input_frame, width=10)
        x_entry.insert(0, "0.0")
        x_entry.grid(row=0, column=1, padx=10, pady=5)
        
        ttk.Label(input_frame, text="Y (m):").grid(row=1, column=0, sticky=tk.W, pady=5)
        y_entry = ttk.Entry(input_frame, width=10)
        y_entry.insert(0, "1.0")
        y_entry.grid(row=1, column=1, padx=10, pady=5)
        
        ttk.Label(input_frame, text="Z (m):").grid(row=2, column=0, sticky=tk.W, pady=5)
        z_entry = ttk.Entry(input_frame, width=10)
        z_entry.insert(0, "0.0")
        z_entry.grid(row=2, column=1, padx=10, pady=5)
        
        ttk.Label(input_frame, text="Yaw (deg):").grid(row=3, column=0, sticky=tk.W, pady=5)
        yaw_entry = ttk.Entry(input_frame, width=10)
        yaw_entry.insert(0, "0.0")
        yaw_entry.grid(row=3, column=1, padx=10, pady=5)
        
        def send_custom_pose():
            try:
                x = float(x_entry.get())
                y = float(y_entry.get())
                z = float(z_entry.get())
                yaw_deg = float(yaw_entry.get())
                
                # Create pose
                translation = Translation3d(x, y, z)
                rotation = Rotation3d(0, 0, math.radians(yaw_deg))
                custom_pose = Pose3d(translation, rotation)
                
                # Send command
                self.questnav.set_pose(custom_pose)
                
                self._log(f"[CMD] Sent pose reset to ({x:.2f}, {y:.2f}, {z:.2f}, yaw={yaw_deg:.1f}°)")
                self._log("      Waiting for command response...")
                
                dialog.destroy()
                
            except ValueError as e:
                messagebox.showerror("Invalid Input", f"Please enter valid numbers:\n{e}")
        
        # Buttons
        btn_frame = ttk.Frame(dialog)
        btn_frame.pack(pady=20)
        
        ttk.Button(btn_frame, text="Send Pose Reset", command=send_custom_pose).pack(side=tk.LEFT, padx=5)
        ttk.Button(btn_frame, text="Cancel", command=dialog.destroy).pack(side=tk.LEFT, padx=5)
    
    def _log(self, msg):
        """Add message to log"""
        if self.log_paused:
            return
        
        ts = datetime.now().strftime("%H:%M:%S.%f")[:-3]
        self.log.config(state='normal')
        self.log.insert(tk.END, f"[{ts}] {msg}\n")
        self.log.see(tk.END)
        self.log.config(state='disabled')
    
    def _clear_log(self):
        """Clear the log"""
        self.log.config(state='normal')
        self.log.delete(1.0, tk.END)
        self.log.config(state='disabled')
        if not self.log_paused:
            self._log("Log cleared")
    
    def _toggle_log_pause(self):
        """Toggle log pause state"""
        self.log_paused = not self.log_paused
        if self.log_paused:
            self.pause_log_btn.config(text="Resume Log")
            self.log.config(state='normal')
            self.log.insert(tk.END, f"[{datetime.now().strftime('%H:%M:%S.%f')[:-3]}] === LOG PAUSED ===\n")
            self.log.see(tk.END)
            self.log.config(state='disabled')
        else:
            self.pause_log_btn.config(text="Pause Log")
            self._log("=== LOG RESUMED ===")
    
    def _export_log(self):
        """Export log to file"""
        from tkinter import filedialog
        
        filename = filedialog.asksaveasfilename(
            defaultextension=".txt",
            filetypes=[("Text files", "*.txt"), ("All files", "*.*")],
            initialfile=f"questnav_log_{datetime.now().strftime('%Y%m%d_%H%M%S')}.txt"
        )
        
        if filename:
            try:
                log_content = self.log.get(1.0, tk.END)
                with open(filename, 'w') as f:
                    f.write(log_content)
                messagebox.showinfo("Export Successful", f"Log exported to:\n{filename}")
                if not self.log_paused:
                    self._log(f"Log exported to {filename}")
            except Exception as e:
                messagebox.showerror("Export Failed", f"Failed to export log:\n{e}")
    
    def run(self):
        """Run the application"""
        try:
            self.root.mainloop()
        finally:
            self._running = False


def main():
    """Main entry point"""
    parser = argparse.ArgumentParser(
        description="QuestNav Viewer - Complete Feature Demonstration",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
This viewer demonstrates ALL features of the questnav-lib Python implementation:

Data Retrieval:
  - get_all_unread_pose_frames()  Real-time pose data
  - get_battery_percent()          Battery monitoring
  - get_frame_count()              Frame counter
  - get_tracking_lost_counter()    Tracking loss events
  - get_latency()                  Connection quality

Status Checks:
  - is_connected()                 Connection monitoring
  - is_tracking()                  Tracking state

Commands:
  - set_pose(pose3d)              Pose reset commands
  - command_periodic()             Response processing

Example:
  python questnav_viewer.py              # Start with defaults
  python questnav_viewer.py --port 5810  # Custom port
        """
    )
    
    parser.add_argument(
        '--port',
        type=int,
        default=5810,
        help='NT4 server port (default: 5810)'
    )
    
    args = parser.parse_args()
    
    try:
        viewer = QuestNavViewer()
        viewer.run()
    except KeyboardInterrupt:
        print("\nShutting down...")
    except Exception as e:
        print(f"ERROR: {e}")
        import traceback
        traceback.print_exc()
        return 1
    
    return 0


if __name__ == '__main__':
    sys.exit(main())
