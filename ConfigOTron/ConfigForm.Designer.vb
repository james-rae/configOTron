﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ConfigForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ConfigForm))
        Me.cmdEnhanceMini = New System.Windows.Forms.Button()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cmdLang = New System.Windows.Forms.Button()
        Me.cmdCopy = New System.Windows.Forms.Button()
        Me.cboEnv = New System.Windows.Forms.ComboBox()
        Me.SuspendLayout()
        '
        'cmdEnhanceMini
        '
        Me.cmdEnhanceMini.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cmdEnhanceMini.Location = New System.Drawing.Point(12, 37)
        Me.cmdEnhanceMini.Name = "cmdEnhanceMini"
        Me.cmdEnhanceMini.Size = New System.Drawing.Size(304, 87)
        Me.cmdEnhanceMini.TabIndex = 1
        Me.cmdEnhanceMini.Text = "Enhance!"
        Me.cmdEnhanceMini.UseVisualStyleBackColor = True
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(67, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(187, 18)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "WMS/WFS Fancy™ Edition"
        '
        'cmdLang
        '
        Me.cmdLang.Location = New System.Drawing.Point(80, 129)
        Me.cmdLang.Name = "cmdLang"
        Me.cmdLang.Size = New System.Drawing.Size(124, 27)
        Me.cmdLang.TabIndex = 3
        Me.cmdLang.Text = "Language Dump"
        Me.cmdLang.UseVisualStyleBackColor = True
        '
        'cmdCopy
        '
        Me.cmdCopy.Location = New System.Drawing.Point(210, 129)
        Me.cmdCopy.Name = "cmdCopy"
        Me.cmdCopy.Size = New System.Drawing.Size(105, 27)
        Me.cmdCopy.TabIndex = 4
        Me.cmdCopy.Text = "Copybot"
        Me.cmdCopy.UseVisualStyleBackColor = True
        '
        'cboEnv
        '
        Me.cboEnv.FormattingEnabled = True
        Me.cboEnv.Items.AddRange(New Object() {"DEV", "PROD"})
        Me.cboEnv.Location = New System.Drawing.Point(12, 133)
        Me.cboEnv.Name = "cboEnv"
        Me.cboEnv.Size = New System.Drawing.Size(62, 21)
        Me.cboEnv.TabIndex = 5
        Me.cboEnv.Text = "DEV"
        '
        'ConfigForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(327, 168)
        Me.Controls.Add(Me.cboEnv)
        Me.Controls.Add(Me.cmdCopy)
        Me.Controls.Add(Me.cmdLang)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.cmdEnhanceMini)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "ConfigForm"
        Me.Text = "Config-O-Tron"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents cmdEnhanceMini As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents cmdLang As Button
    Friend WithEvents cmdCopy As Button
    Friend WithEvents cboEnv As ComboBox
End Class
